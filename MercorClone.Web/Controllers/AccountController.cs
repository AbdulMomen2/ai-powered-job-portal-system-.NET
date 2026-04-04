using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MercorClone.Web.Models.Entities;
using MercorClone.Web.Models.ViewModels;
using MercorClone.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MercorClone.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly AppDbContext _context;
        

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────
        // REGISTER
        // ─────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Register(string? role = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard();

            // Pre-select role if arriving from the "Post a Job" flow
            var model = new RegisterViewModel();
            if (role == "Employer") model.UserType = "Employer";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Security guard: only accept known UserType values — never trust client input blindly
            if (model.UserType != "Candidate" && model.UserType != "Employer")
            {
                ModelState.AddModelError(nameof(model.UserType), "Please select a valid account type.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName  = model.Email,
                Email     = model.Email,
                FullName  = model.FullName,
                UserType  = model.UserType,
                // PhoneNumber = model.PhoneNumber // stored natively on IdentityUser
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("New {UserType} account created: {Email}", user.UserType, user.Email);

                await _signInManager.SignInAsync(user, isPersistent: false);
                await SyncUserTypeClaim(user);

                return RedirectToDashboard(user.UserType);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ─────────────────────────────────────────────────────────────
        // LOGIN
        // ─────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard();

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null) return RedirectToAction("Index", "Home");

                await SyncUserTypeClaim(user);
                _logger.LogInformation("{Email} signed in as {UserType}", user.Email, user.UserType);

                // Safe returnUrl routing:
                // A Candidate cannot be pushed into /Employer/* via a crafted returnUrl link.
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    bool isEmployerUrl  = returnUrl.StartsWith("/Employer",  StringComparison.OrdinalIgnoreCase);
                    bool isCandidateUrl = returnUrl.StartsWith("/Candidate", StringComparison.OrdinalIgnoreCase);

                    bool roleAllowed =
                        (!isEmployerUrl && !isCandidateUrl) ||
                        (isEmployerUrl  && user.UserType == "Employer")  ||
                        (isCandidateUrl && user.UserType == "Candidate");

                    if (roleAllowed)
                        return Redirect(returnUrl);

                    // They tried to access the wrong role's URL — redirect to their own dashboard
                    TempData["Warning"] = "You don't have access to that area.";
                }

                return RedirectToDashboard(user.UserType);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Locked out login attempt: {Email}", model.Email);
                ModelState.AddModelError(string.Empty,
                    "Account temporarily locked after multiple failed attempts. Please try again in a few minutes.");
                return View(model);
            }

            if (result.RequiresTwoFactor)
                return RedirectToPage("/Account/LoginWith2fa", new { ReturnUrl = returnUrl, model.RememberMe });

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // ─────────────────────────────────────────────────────────────
        // LOGOUT
        // ─────────────────────────────────────────────────────────────

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("{User} logged out.", User.Identity?.Name);
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ─────────────────────────────────────────────────────────────
        // ACCOUNT SETTINGS (shared – both roles)
        // ─────────────────────────────────────────────────────────────

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index()
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    ViewBag.UserType       = user?.UserType ?? "";
                    ViewBag.UserFullName   = user?.FullName ?? "My Account";
                    ViewBag.UserInitial    = user?.FullName?.Substring(0, 1).ToUpper() ?? "?";
                    ViewBag.PhoneVerified  = user?.PhoneNumberConfirmed ?? false;
                }
                var jobs = await _context.JobPosts
                    .Include(j => j.Company)
                    .Where(j => j.Status == "Active")
                    .OrderByDescending(j => j.PublishedAt)
                    .ToListAsync();
                return View(jobs);
            }

        // ─────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Keeps the "UserType" claim in the identity cookie in sync with the DB value.
        /// This lets Razor views branch by role without hitting the DB on every request.
        /// </summary>
        private async Task SyncUserTypeClaim(ApplicationUser user)
        {
            var existing = await _userManager.GetClaimsAsync(user);
            var old = existing.FirstOrDefault(c => c.Type == "UserType");
            if (old != null) await _userManager.RemoveClaimAsync(user, old);
            await _userManager.AddClaimAsync(user, new Claim("UserType", user.UserType ?? "Candidate"));
        }

        private IActionResult RedirectToDashboard(string? userType = null)
        {
            userType ??= User.FindFirstValue("UserType");
            return userType == "Employer"
                ? RedirectToAction("Dashboard", "Employer")
                : RedirectToAction("Dashboard", "Candidate");
        }
    }
}