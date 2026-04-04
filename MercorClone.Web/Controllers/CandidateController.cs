using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MercorClone.Web.Data;
using MercorClone.Web.Models.Entities;
using MercorClone.Web.Models.ViewModels.Candidate;

namespace MercorClone.Web.Controllers
{
    [Authorize]
    public class CandidateController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CandidateController> _logger;

        public CandidateController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<CandidateController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────
        // ROLE GUARD — mirrors the same pattern in EmployerController
        // ─────────────────────────────────────────────────────────────
        private async Task<(ApplicationUser? user, IActionResult? redirect)> RequireCandidate()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return (null, RedirectToAction("Login", "Account"));

            if (user.UserType == "Employer")
            {
                _logger.LogWarning("Employer {Email} tried to access a candidate route.", user.Email);
                TempData["Error"] = "That area is for candidates only.";
                return (null, RedirectToAction("Dashboard", "Employer"));
            }

            return (user, null);
        }

        // ─────────────────────────────────────────────────────────────
        // DASHBOARD
        // ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Dashboard()
        {
            var (user, redirect) = await RequireCandidate();
            if (redirect != null) return redirect;

            // Load their profile + mastery in one query
            var profile = await _context.CandidateProfiles
                .Include(c => c.Mastery)
                .Include(c => c.Experiences.OrderByDescending(e => e.StartDate))
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);

            // Load their job applications with company info
            List<JobApplication> applications = new();
            if (profile != null)
            {
                applications = await _context.JobApplications
                    .Include(a => a.JobPost)
                        .ThenInclude(j => j.Company)
                    .Where(a => a.CandidateProfileId == profile.Id)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10) // show the 10 most recent on the dashboard
                    .ToListAsync();
            }

            // Recommended jobs — naive: match by skill tags if mastery exists
            List<JobPost> recommended = new();
            if (profile?.Mastery?.SkillTags.Length > 0)
            {
                recommended = await _context.JobPosts
                    .Include(j => j.Company)
                    .Where(j => j.Status == "Active" &&
                                j.RequiredSkills != null &&
                                j.RequiredSkills.Any(s => profile.Mastery.SkillTags.Contains(s)))
                    .OrderByDescending(j => j.PublishedAt)
                    .Take(6)
                    .ToListAsync();
            }

            ViewBag.UserFullName      = user!.FullName;
            ViewBag.UserEmail         = user.Email;
            ViewBag.PhoneVerified     = user.PhoneNumberConfirmed;
            ViewBag.ProfileComplete   = profile != null;
            ViewBag.Profile           = profile;
            ViewBag.RecommendedJobs   = recommended;
            ViewBag.ApplicationCount  = applications.Count;
            ViewBag.ActiveApplications = applications.Count(a => a.Status is "Applied" or "Shortlisted" or "Interview");

            return View(applications);
        }

        // ─────────────────────────────────────────────────────────────
        // JOB ACTIVITY (all applications, paginated)
        // ─────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> JobActivity(int page = 1)
        {
            var (user, redirect) = await RequireCandidate();
            if (redirect != null) return redirect;

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);

            List<JobApplication> applications = new();
            if (profile != null)
            {
                const int pageSize = 12;
                applications = await _context.JobApplications
                    .Include(a => a.JobPost).ThenInclude(j => j.Company)
                    .Where(a => a.CandidateProfileId == profile.Id)
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }

            ViewBag.UserFullName = user!.FullName;
            ViewBag.UserType     = user.UserType;
            return View(applications);
        }

        // ─────────────────────────────────────────────────────────────
        // APPLY FOR A JOB
        // ─────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(Guid jobId)
        {
            var (user, redirect) = await RequireCandidate();
            if (redirect != null) return redirect;

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);

            if (profile == null)
            {
                TempData["Error"] = "Please complete your candidate profile before applying.";
                return RedirectToAction(nameof(EditProfile));
            }

            var job = await _context.JobPosts.FindAsync(jobId);
            if (job == null || job.Status != "Active")
            {
                TempData["Error"] = "This job is no longer available.";
                return RedirectToAction("Index", "Home");
            }

            // Duplicate application guard (DB has a unique index but we check here for a friendly error)
            var alreadyApplied = await _context.JobApplications
                .AnyAsync(a => a.JobPostId == jobId && a.CandidateProfileId == profile.Id);

            if (alreadyApplied)
            {
                TempData["Warning"] = "You have already applied for this position.";
                return RedirectToAction("Index", "Home");
            }

            var application = new JobApplication
            {
                JobPostId          = jobId,
                CandidateProfileId = profile.Id,
                Status             = "Applied",
                LastStatusUpdate   = DateTime.UtcNow
            };

            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Candidate {Email} applied for job {JobId}", user!.Email, jobId);

            TempData["Success"] = $"Application submitted for '{job.Title}'.";
            return RedirectToAction(nameof(Dashboard));
        }

        // ─────────────────────────────────────────────────────────────
        // PROFILE — VIEW & EDIT (CV, Skills, Experience)
        // ─────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var (user, redirect) = await RequireCandidate();
            if (redirect != null) return redirect;

            var profile = await _context.CandidateProfiles
                .Include(c => c.Mastery)
                .Include(c => c.Experiences)
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);

            ViewBag.UserFullName  = user!.FullName;
            ViewBag.UserEmail     = user.Email;
            ViewBag.UserType      = user.UserType;
            ViewBag.PhoneVerified = user.PhoneNumberConfirmed;
            ViewBag.Phone         = user.PhoneNumber;
            return View(profile); // View handles both create-new and edit scenarios
        }

        // ─────────────────────────────────────────────────────────────
        // JOB PREFERENCES
        // ─────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> JobPreferences()
        {
            var (user, redirect) = await RequireCandidate();
            if (redirect != null) return redirect;

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);

            ViewBag.UserFullName = user!.FullName;
            ViewBag.UserType     = user.UserType;
            return View(profile);
        }
    }
}