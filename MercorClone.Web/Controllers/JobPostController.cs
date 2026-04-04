using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MercorClone.Web.Data;
using MercorClone.Web.Models.Entities;
using MercorClone.Web.Models.ViewModels.Employer;

namespace MercorClone.Web.Controllers
{
    [Authorize]
    [Route("employer/jobs")]
    public class JobPostController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<JobPostController> _logger;
        private readonly IHtmlSanitizer _sanitizer;

        public JobPostController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<JobPostController> logger,
            IHtmlSanitizer sanitizer)
        {
            _context     = context;
            _userManager = userManager;
            _logger      = logger;
            _sanitizer   = sanitizer;
        }


        private async Task<(ApplicationUser? user, EmployerProfile? profile)> ResolveEmployerAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
            {
                _logger.LogWarning("Unauthenticated request to JobPostController.");
                return (null, null);
            }

            if (user.UserType != "Employer")
            {
                _logger.LogWarning(
                    "Non-employer user {Email} ({UserType}) attempted to access JobPostController.",
                    user.Email, user.UserType);
                TempData["Error"] = "That area is for employers only.";
                return (null, null);
            }

            var profile = await _context.EmployerProfiles
                .Include(e => e.Company)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            return (user, profile);
        }

        private void ApplyBusinessRules(CreateJobViewModel model)
        {
            if (model.MinSalary > 0 && model.MaxSalary > 0 && model.MaxSalary <= model.MinSalary)
            {
                ModelState.AddModelError(
                    nameof(model.MaxSalary),
                    "Maximum salary must be greater than minimum salary.");
            }

            if (model.LocationType != "Remote" && string.IsNullOrWhiteSpace(model.Location))
            {
                ModelState.AddModelError(
                    nameof(model.Location),
                    "A specific office location is required for On-site or Hybrid roles.");
            }

            var plainText = System.Text.RegularExpressions.Regex
                .Replace(model.JobDescriptionHtml ?? string.Empty, "<[^>]*>", string.Empty)
                .Trim();

            if (plainText.Length < 50)
            {
                ModelState.AddModelError(
                    nameof(model.JobDescriptionHtml),
                    "Please provide a meaningful job description (at least 50 characters of real content).");
            }

            if ((model.Latitude.HasValue && !model.Longitude.HasValue) ||
                (!model.Latitude.HasValue && model.Longitude.HasValue))
            {
                ModelState.AddModelError(string.Empty,
                    "Both latitude and longitude must be provided together.");
            }

            var skills = (model.RequiredSkills ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (skills.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.RequiredSkills),
                    "Please list at least one required skill.");
            }
        }

        // ─────────────────────────────────────────────────────────────
        // GET /employer/jobs/create
        // ─────────────────────────────────────────────────────────────

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            var (user, profile) = await ResolveEmployerAsync();

            if (user is null)    return RedirectToAction("Login", "Account");
            if (profile is null) return RedirectToAction("SetupCompany", "Employer");

            return View(new CreateJobViewModel());
        }

        // ─────────────────────────────────────────────────────────────
        // POST /employer/jobs/create
        // ─────────────────────────────────────────────────────────────

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateJobViewModel model)
        {
            var (user, profile) = await ResolveEmployerAsync();

            if (user is null)    return RedirectToAction("Login", "Account");
            if (profile is null) return RedirectToAction("SetupCompany", "Employer");

            ApplyBusinessRules(model);

            if (!ModelState.IsValid)
                return View(model);

            var safeHtml = _sanitizer.Sanitize(model.JobDescriptionHtml);

            var skills = (model.RequiredSkills ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            try
            {
                var jobPost = new JobPost
                {
                    CompanyId          = profile.CompanyId,
                    PostedByUserId     = user.Id,
                    Title              = model.Title.Trim(),
                    Category           = model.Category,
                    ContractType       = model.ContractType,
                    LocationType       = model.LocationType,
                    Location           = model.LocationType == "Remote" ? null : model.Location?.Trim(),
                    Country            = model.Country?.Trim(),
                    Latitude           = model.Latitude,
                    Longitude          = model.Longitude,
                    WorkingHours       = model.WorkingHours.Trim(),
                    MinSalary          = model.MinSalary,
                    MaxSalary          = model.MaxSalary,
                    HiresTarget        = model.HiresTarget,
                    JobDescriptionHtml = safeHtml,
                    RequiredSkills     = skills,
                    Status             = "Active",
                    PublishedAt        = DateTime.UtcNow
                };

                _context.JobPosts.Add(jobPost);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Job '{Title}' (ID: {JobId}) posted by {Email} for company {CompanyId}.",
                    jobPost.Title, jobPost.Id, user.Email, profile.CompanyId);

                TempData["Success"] = $"'{jobPost.Title}' has been posted successfully!";
                return RedirectToAction("Dashboard", "Employer");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Database error while creating job post for company {CompanyId} by {Email}.",
                    profile.CompanyId, user.Email);
                ModelState.AddModelError(string.Empty,
                    "A database error occurred while saving your job post. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating job post for {Email}.", user.Email);
                ModelState.AddModelError(string.Empty,
                    "An unexpected error occurred. Please try again or contact support.");
                return View(model);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // GET /employer/jobs/{jobId}/edit
        // ─────────────────────────────────────────────────────────────

        [HttpGet("{jobId:guid}/edit")]
        public async Task<IActionResult> Edit(Guid jobId)
        {
            var (user, profile) = await ResolveEmployerAsync();
            if (user is null)    return RedirectToAction("Login", "Account");
            if (profile is null) return RedirectToAction("SetupCompany", "Employer");

            var job = await _context.JobPosts
                .FirstOrDefaultAsync(j => j.Id == jobId && j.CompanyId == profile.CompanyId);

            if (job is null)
            {
                TempData["Error"] = "Job not found or you do not have permission to edit it.";
                return RedirectToAction("Dashboard", "Employer");
            }

            if (job.Status == "Closed")
            {
                TempData["Error"] = "Closed jobs cannot be edited. Reopen the job first.";
                return RedirectToAction("Dashboard", "Employer");
            }

            var model = new CreateJobViewModel
            {
                Title              = job.Title,
                Category           = job.Category,
                ContractType       = job.ContractType,
                LocationType       = job.LocationType,
                Location           = job.Location,
                Country            = job.Country,
                Latitude           = job.Latitude,
                Longitude          = job.Longitude,
                WorkingHours       = job.WorkingHours,
                MinSalary          = job.MinSalary ?? 0,
                MaxSalary          = job.MaxSalary ?? 0,
                HiresTarget        = job.HiresTarget,
                RequiredSkills     = job.RequiredSkills is { Length: > 0 }
                                        ? string.Join(", ", job.RequiredSkills)
                                        : string.Empty,
                JobDescriptionHtml = job.JobDescriptionHtml
            };

            ViewBag.JobId = jobId;
            return View("Create", model);
        }

        // ─────────────────────────────────────────────────────────────
        // POST /employer/jobs/{jobId}/edit
        // ─────────────────────────────────────────────────────────────

        [HttpPost("{jobId:guid}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid jobId, CreateJobViewModel model)
        {
            var (user, profile) = await ResolveEmployerAsync();
            if (user is null)    return RedirectToAction("Login", "Account");
            if (profile is null) return RedirectToAction("SetupCompany", "Employer");

            ApplyBusinessRules(model);

            if (!ModelState.IsValid)
            {
                ViewBag.JobId = jobId;
                return View("Create", model);
            }

            var job = await _context.JobPosts
                .FirstOrDefaultAsync(j => j.Id == jobId && j.CompanyId == profile.CompanyId);

            if (job is null)
            {
                TempData["Error"] = "Job not found or access denied.";
                return RedirectToAction("Dashboard", "Employer");
            }

            try
            {
                var safeHtml = _sanitizer.Sanitize(model.JobDescriptionHtml);
                var skills   = (model.RequiredSkills ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                job.Title              = model.Title.Trim();
                job.Category           = model.Category;
                job.ContractType       = model.ContractType;
                job.LocationType       = model.LocationType;
                job.Location           = model.LocationType == "Remote" ? null : model.Location?.Trim();
                job.Country            = model.Country?.Trim();
                job.Latitude           = model.Latitude;
                job.Longitude          = model.Longitude;
                job.WorkingHours       = model.WorkingHours.Trim();
                job.MinSalary          = model.MinSalary;
                job.MaxSalary          = model.MaxSalary;
                job.HiresTarget        = model.HiresTarget;
                job.JobDescriptionHtml = safeHtml;
                job.RequiredSkills     = skills;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Job '{Title}' (ID: {JobId}) updated by {Email}.",
                    job.Title, job.Id, user.Email);

                TempData["Success"] = $"'{job.Title}' has been updated.";
                return RedirectToAction("Dashboard", "Employer");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict editing job {JobId}.", jobId);
                ModelState.AddModelError(string.Empty,
                    "This job was modified by another session. Please refresh and try again.");
                ViewBag.JobId = jobId;
                return View("Create", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error editing job {JobId}.", jobId);
                ModelState.AddModelError(string.Empty,
                    "An unexpected error occurred. Please try again.");
                ViewBag.JobId = jobId;
                return View("Create", model);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // POST /employer/jobs/{jobId}/close
        // ─────────────────────────────────────────────────────────────

        [HttpPost("{jobId:guid}/close")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(Guid jobId)
        {
            var (user, profile) = await ResolveEmployerAsync();
            if (user is null)    return RedirectToAction("Login", "Account");
            if (profile is null) return RedirectToAction("Dashboard", "Employer");

            var job = await _context.JobPosts
                .FirstOrDefaultAsync(j => j.Id == jobId && j.CompanyId == profile.CompanyId);

            if (job is null)
            {
                TempData["Error"] = "Job not found or access denied.";
                return RedirectToAction("Dashboard", "Employer");
            }

            if (job.Status == "Closed")
            {
                TempData["Info"] = "This job is already closed.";
                return RedirectToAction("Dashboard", "Employer");
            }

            try
            {
                job.Status   = "Closed";
                job.ClosedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Job '{Title}' (ID: {JobId}) closed by {Email}.",
                    job.Title, job.Id, user.Email);

                TempData["Success"] = $"'{job.Title}' has been closed.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing job {JobId}.", jobId);
                TempData["Error"] = "Could not close the job. Please try again.";
            }

            return RedirectToAction("Dashboard", "Employer");
        }

        // ─────────────────────────────────────────────────────────────
        // POST /employer/jobs/{jobId}/reopen
        // ─────────────────────────────────────────────────────────────

        [HttpPost("{jobId:guid}/reopen")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(Guid jobId)
        {
            var (user, profile) = await ResolveEmployerAsync();
            if (user is null)    return RedirectToAction("Login", "Account");
            if (profile is null) return RedirectToAction("Dashboard", "Employer");

            var job = await _context.JobPosts
                .FirstOrDefaultAsync(j => j.Id == jobId && j.CompanyId == profile.CompanyId);

            if (job is null)
            {
                TempData["Error"] = "Job not found or access denied.";
                return RedirectToAction("Dashboard", "Employer");
            }

            if (job.Status == "Active")
            {
                TempData["Info"] = "This job is already active.";
                return RedirectToAction("Dashboard", "Employer");
            }

            try
            {
                job.Status      = "Active";
                job.ClosedAt    = null;
                job.PublishedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Job '{Title}' (ID: {JobId}) reopened by {Email}.",
                    job.Title, job.Id, user.Email);

                TempData["Success"] = $"'{job.Title}' is now active again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reopening job {JobId}.", jobId);
                TempData["Error"] = "Could not reopen the job. Please try again.";
            }

            return RedirectToAction("Dashboard", "Employer");
        }

        // ─────────────────────────────────────────────────────────────
        // GET /employer/jobs/{jobId}/applicants
        // ─────────────────────────────────────────────────────────────

        [HttpGet("{jobId:guid}/applicants")]
        public async Task<IActionResult> Applicants(Guid jobId, int page = 1, int pageSize = 20)
        {
            var (user, profile) = await ResolveEmployerAsync();
            if (user is null)    return RedirectToAction("Login", "Account");
            if (profile is null) return RedirectToAction("SetupCompany", "Employer");

            pageSize = Math.Clamp(pageSize, 5, 100);
            page     = Math.Max(1, page);

            var job = await _context.JobPosts.AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobId && j.CompanyId == profile.CompanyId);

            if (job is null)
            {
                TempData["Error"] = "Job not found.";
                return RedirectToAction("Dashboard", "Employer");
            }

            var query = _context.JobApplications
                .Include(a => a.CandidateProfile)
                    .ThenInclude(cp => cp.User)
                .Include(a => a.CandidateProfile)
                    .ThenInclude(cp => cp.Mastery)
                .Where(a => a.JobPostId == jobId)
                .OrderByDescending(a => a.AiMatchScore)
                .ThenByDescending(a => a.CreatedAt);

            var totalCount   = await query.CountAsync();
            var applications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.JobTitle   = job.Title;
            ViewBag.JobId      = jobId;
            ViewBag.CompanyName = profile.Company.Name;
            ViewBag.TotalCount = totalCount;
            ViewBag.Page       = page;
            ViewBag.PageSize   = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return View(applications);
        }
    }
}