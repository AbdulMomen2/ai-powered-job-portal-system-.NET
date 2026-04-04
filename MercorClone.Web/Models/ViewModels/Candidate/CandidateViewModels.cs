using System.ComponentModel.DataAnnotations;
using MercorClone.Web.Models.Entities;

namespace MercorClone.Web.Models.ViewModels.Candidate
{
    // ─────────────────────────────────────────────────────────────
    //  Used by CandidateController.Dashboard
    //  Aggregates all data the dashboard view needs in one shot —
    //  no lazy-loading, no extra queries from the view.
    // ─────────────────────────────────────────────────────────────
    public class CandidateDashboardViewModel
    {
        // Identity
        public string FullName { get; set; } = string.Empty;
        public string Email    { get; set; } = string.Empty;

        // Profile (null = not yet set up → onboarding redirect)
        public CandidateProfile? Profile { get; set; }

        // Recent applications (last 10, ordered by CreatedAt desc)
        public IEnumerable<JobApplication> Applications { get; set; }
            = Enumerable.Empty<JobApplication>();

        // Skill-matched job recommendations
        public IEnumerable<JobPost> RecommendedJobs { get; set; }
            = Enumerable.Empty<JobPost>();
    }

    // ─────────────────────────────────────────────────────────────
    //  Used by CandidateController.SetupProfile  (POST + GET)
    //         CandidateController.AccountSettings (POST + GET)
    // ─────────────────────────────────────────────────────────────
    public class CandidateProfileSetupViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20)]
        [Display(Name = "Phone / WhatsApp Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Professional Summary")]
        public string? ProfileSummary { get; set; }

        [StringLength(100)]
        [Display(Name = "Country")]
        public string? Country { get; set; }

        [StringLength(100)]
        [Display(Name = "City")]
        public string? City { get; set; }

        /// <summary>
        /// Comma-separated skills, e.g. "Python, React, SQL".
        /// Split into string[] in the controller before saving to CandidateMastery.SkillTags.
        /// </summary>
        [Display(Name = "Skills (comma-separated)")]
        public string? SkillsCsv { get; set; }
    }
}