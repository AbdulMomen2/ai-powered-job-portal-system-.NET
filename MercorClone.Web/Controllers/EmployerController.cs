using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MercorClone.Web.Data;
using MercorClone.Web.Models.Entities;
using MercorClone.Web.Models.ViewModels.Employer;

namespace MercorClone.Web.Controllers
{
    /// <summary>
    /// Handles employer profile management (dashboard, company setup, applicant review).
    /// Job post CRUD has been extracted to <see cref="JobPostController"/>.
    /// </summary>
    [Authorize]
    public class EmployerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EmployerController> _logger;

        public EmployerController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<EmployerController> logger)
        {
            _context     = context;
            _userManager = userManager;
            _logger      = logger;
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves and validates the current user is an authenticated employer.
        /// Returns (null, redirect) when the caller should immediately return the redirect.
        /// </summary>
        private async Task<(ApplicationUser? user, IActionResult? redirect)> RequireEmployerAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return (null, RedirectToAction("Login", "Account"));

            if (user.UserType != "Employer")
            {
                _logger.LogWarning(
                    "Unauthorized employer access by {UserType} user {Email}.",
                    user.UserType, user.Email);

                TempData["Error"] = "That area is for employers only.";
                return (null, RedirectToAction("Dashboard", "Candidate"));
            }

            return (user, null);
        }

        // ─────────────────────────────────────────────────────────────
        // DASHBOARD
        // ─────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var (user, redirect) = await RequireEmployerAsync();
            if (redirect is not null) return redirect;

            try
            {
                var employerProfile = await _context.EmployerProfiles
                    .Include(e => e.Company)
                        .ThenInclude(c => c.Subscription)
                            .ThenInclude(s => s!.SubscriptionPlan)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.UserId == user!.Id);

                if (employerProfile is null)
                    return RedirectToAction(nameof(SetupCompany));

                var activeJobs = await _context.JobPosts
                    .Include(j => j.Applications)
                    .Where(j => j.CompanyId == employerProfile.CompanyId)
                    .OrderByDescending(j => j.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();

                ViewBag.CompanyName        = employerProfile.Company.Name;
                ViewBag.CompanyId          = employerProfile.CompanyId;
                ViewBag.UserFullName       = user!.FullName;
                ViewBag.TotalApplicants    = activeJobs.Sum(j => j.Applications.Count);
                ViewBag.ActiveJobCount     = activeJobs.Count(j => j.Status == "Active");
                ViewBag.SubscriptionStatus = employerProfile.Company.Subscription?.Status ?? "Free";

                return View(activeJobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard for {Email}.", user!.Email);
                TempData["Error"] = "Could not load the dashboard. Please refresh.";
                return View(new List<JobPost>());
            }
        }
        [HttpGet]
        public IActionResult CreateJob() => RedirectToAction("Create", "JobPost");
        // ─────────────────────────────────────────────────────────────
        // COMPANY SETUP (ONBOARDING)
        // ─────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> SetupCompany()
        {
            var (user, redirect) = await RequireEmployerAsync();
            if (redirect is not null) return redirect;

            var exists = await _context.EmployerProfiles.AnyAsync(e => e.UserId == user!.Id);
            if (exists) return RedirectToAction(nameof(Dashboard));

            return View(new CompanySetupViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetupCompany(CompanySetupViewModel model)
        {
            var (user, redirect) = await RequireEmployerAsync();
            if (redirect is not null) return redirect;

            if (!ModelState.IsValid) return View(model);

            // Prevent duplicate setup on double-submit
            var alreadyExists = await _context.EmployerProfiles.AnyAsync(e => e.UserId == user!.Id);
            if (alreadyExists) return RedirectToAction(nameof(Dashboard));

            try
            {
                var company = new Company
                {
                    Name               = model.CompanyName.Trim(),
                    WebsiteUrl         = model.WebsiteUrl?.Trim(),
                    Industry           = model.Industry?.Trim(),
                    CompanyDescription = model.CompanyDescription?.Trim()
                };
                _context.Companies.Add(company);

                var profile = new EmployerProfile
                {
                    UserId           = user!.Id,
                    Company          = company,
                    JobTitle         = model.YourJobTitle.Trim(),
                    IsPrimaryContact = true
                };
                _context.EmployerProfiles.Add(profile);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Company '{Name}' created by {Email}.", company.Name, user.Email);

                TempData["Success"] = $"Welcome! '{company.Name}' is ready. Now post your first job.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error creating company for {Email}.", user!.Email);
                ModelState.AddModelError(string.Empty,
                    "A database error occurred. Please try again.");
                return View(model);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PUBLIC LANDING
        // ─────────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public IActionResult HireLanding() => View();
    }
}
