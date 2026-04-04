using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MercorClone.Web.Data;
using MercorClone.Web.Models.Entities;

namespace MercorClone.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context     = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // ── Populate user context for navbar / profile dropdown ──
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ViewBag.UserType      = user.UserType ?? "";
                    ViewBag.UserFullName  = user.FullName ?? "My Account";
                    ViewBag.UserInitial   = !string.IsNullOrEmpty(user.FullName)
                                               ? user.FullName.Substring(0, 1).ToUpper()
                                               : "?";
                    ViewBag.PhoneVerified = user.PhoneNumberConfirmed;
                }
            }

            // ── Job board data ──
            var activeJobs = await _context.JobPosts
                .Include(j => j.Company)
                .Where(j => j.Status == "Active")
                .OrderByDescending(j => j.PublishedAt)
                .ToListAsync();

            return View(activeJobs);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new MercorClone.Web.Models.ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
    }
}