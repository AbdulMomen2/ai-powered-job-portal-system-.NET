using System.ComponentModel.DataAnnotations;

namespace MercorClone.Web.Models.ViewModels.Employer
{
    // ─────────────────────────────────────────────────────────────
    //  Used by EmployerController.SetupCompany
    //  Shown once after an employer registers, before they can
    //  post jobs or access the dashboard.
    // ─────────────────────────────────────────────────────────────
    public class CompanySetupViewModel
    {
        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 100 characters.")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Url(ErrorMessage = "Enter a valid URL (e.g. https://yourcompany.com).")]
        [Display(Name = "Website URL")]
        public string? WebsiteUrl { get; set; }

        [StringLength(50)]
        [Display(Name = "Industry")]
        public string? Industry { get; set; }

        [StringLength(2000)]
        [Display(Name = "Company Description")]
        public string? CompanyDescription { get; set; }

        [Required(ErrorMessage = "Your job title is required.")]
        [StringLength(100)]
        [Display(Name = "Your Job Title")]
        public string YourJobTitle { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────
    //  Used by EmployerController.CreateJob
//     // ─────────────────────────────────────────────────────────────
//     public class CreateJobViewModel
//     {
//         [Required(ErrorMessage = "Job title is required.")]
//         [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
//         [Display(Name = "Job Title")]
//         public string Title { get; set; } = string.Empty;

//         [Required(ErrorMessage = "Category is required.")]
//         [StringLength(100)]
//         [Display(Name = "Category")]
//         public string Category { get; set; } = string.Empty;

//         [Required(ErrorMessage = "Contract type is required.")]
//         [Display(Name = "Contract Type")]
//         public string ContractType { get; set; } = "Full-time";

//         [Required(ErrorMessage = "Location type is required.")]
//         [Display(Name = "Location Type")]
//         public string LocationType { get; set; } = "Remote";

//         [Display(Name = "Specific Location")]
//         public string? Location { get; set; }

//         [StringLength(50)]
//         [Display(Name = "Working Hours")]
//         public string WorkingHours { get; set; } = string.Empty;

//         [Range(0, 10_000_000, ErrorMessage = "Enter a valid minimum salary.")]
//         [Display(Name = "Min Salary ($/hr)")]
//         public decimal? MinSalary { get; set; }

//         [Range(0, 10_000_000, ErrorMessage = "Enter a valid maximum salary.")]
//         [Display(Name = "Max Salary ($/hr)")]
//         public decimal? MaxSalary { get; set; }

//         [Range(1, 1000, ErrorMessage = "Hires target must be between 1 and 1000.")]
//         [Display(Name = "Hires Target")]
//         public int HiresTarget { get; set; } = 1;

//         [Required(ErrorMessage = "Job description is required.")]
//         [Display(Name = "Job Description")]
//         public string JobDescriptionHtml { get; set; } = string.Empty;

//         /// <summary>
//         /// Comma-separated list of skills, e.g. "React, Node.js, TypeScript".
//         /// Parsed into string[] in the controller before saving.
//         /// </summary>
//         [Display(Name = "Required Skills (comma-separated)")]
//         public string? RequiredSkills { get; set; }
//     }
}