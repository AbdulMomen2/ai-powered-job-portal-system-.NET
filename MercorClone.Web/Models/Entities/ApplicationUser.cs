using Microsoft.AspNetCore.Identity;

namespace MercorClone.Web.Models.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public required string FullName { get; set; }
        public string? ProfileImageurl { get; set; }
        public string? UserType { get; set;} // "SuperAdmin", "Employer", "Candidate"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        public CandidateProfile? CandidateProfile { get; set; }
        public EmployerProfile? EmployerProfile { get; set; }
    }
}