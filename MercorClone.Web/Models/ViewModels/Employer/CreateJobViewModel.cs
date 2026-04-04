using System.ComponentModel.DataAnnotations;

namespace MercorClone.Web.Models.ViewModels.Employer
{
    /// <summary>
    /// View model for creating or editing a job post.
    /// Business-rule cross-field validation (salary range, location) is
    /// handled in the controller via ApplyBusinessRules() because it
    /// requires context that annotations cannot express cleanly.
    /// </summary>
    public class CreateJobViewModel
    {
        // ── Step 1: Role Basics ──────────────────────────────────────

        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(200, MinimumLength = 5,
            ErrorMessage = "Job title must be between 5 and 200 characters.")]
        [Display(Name = "Job Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a work setting.")]
        [Display(Name = "Work Setting")]
        public string LocationType { get; set; } = "Remote";

        /// <summary>
        /// Required when LocationType is not "Remote".
        /// Validated in controller via ApplyBusinessRules.
        /// </summary>
        [StringLength(200)]
        [Display(Name = "Office Location")]
        public string? Location { get; set; }

        [StringLength(100)]
        [Display(Name = "Country")]
        public string? Country { get; set; }

        /// <summary>Captured from map pin drag. Optional but must be a valid pair.</summary>
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double? Latitude { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double? Longitude { get; set; }

        // ── Step 2: Logistics ────────────────────────────────────────

        [Required(ErrorMessage = "Please select an employment type.")]
        [Display(Name = "Employment Type")]
        public string ContractType { get; set; } = "Full-time";

        [Required(ErrorMessage = "Working hours / timezone is required.")]
        [StringLength(100)]
        [Display(Name = "Working Hours / Timezone")]
        public string WorkingHours { get; set; } = string.Empty;

        [Required(ErrorMessage = "Number of hires is required.")]
        [Range(1, 1000, ErrorMessage = "Hires target must be between 1 and 1000.")]
        [Display(Name = "Number of Hires")]
        public int HiresTarget { get; set; } = 1;

        // ── Step 3: Compensation & Skills ────────────────────────────

        [Required(ErrorMessage = "Minimum salary is required.")]
        [Range(1, 10_000_000, ErrorMessage = "Please enter a valid minimum salary.")]
        [Display(Name = "Minimum Annual Salary (USD)")]
        public decimal MinSalary { get; set; }

        [Required(ErrorMessage = "Maximum salary is required.")]
        [Range(1, 10_000_000, ErrorMessage = "Please enter a valid maximum salary.")]
        [Display(Name = "Maximum Annual Salary (USD)")]
        public decimal MaxSalary { get; set; }

        /// <summary>
        /// Comma-separated skills string from the UI.
        /// Parsed into string[] in the controller. At least one skill required.
        /// </summary>
        [Required(ErrorMessage = "At least one required skill must be specified.")]
        [StringLength(1000)]
        [Display(Name = "Required Skills (comma-separated)")]
        public string RequiredSkills { get; set; } = string.Empty;

        // ── Step 4: Job Description ──────────────────────────────────

        /// <summary>
        /// Raw HTML from Quill.js editor.
        /// Sanitized server-side before persisting (prevents stored XSS).
        /// The [Required] catches an empty submit; real length validation
        /// uses plain-text extraction in ApplyBusinessRules.
        /// </summary>
        [Required(ErrorMessage = "Job description is required.")]
        [Display(Name = "Job Description")]
        public string JobDescriptionHtml { get; set; } = string.Empty;
    }
}
