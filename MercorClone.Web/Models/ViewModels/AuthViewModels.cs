using System.ComponentModel.DataAnnotations;

namespace MercorClone.Web.Models.ViewModels
{
    public class RegisterViewModel
    {[Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;[Required(ErrorMessage = "Password is required.")][StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)][DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;[Required(ErrorMessage = "Please select an account type.")]
        public string UserType { get; set; } = "Candidate"; 
    }

    public class LoginViewModel
    {[Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;[Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}