using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MercorClone.Web.Models.Entities
{   
    [Index(nameof(UserId), IsUnique = true)] 
    [Index(nameof(Country), nameof(City))] // Critical for geographic matching
    public class CandidateProfile : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required, Phone, Column(TypeName = "varchar(20)")] 
        public string PhoneNumber { get; set; } = string.Empty;

        public string? ProfileSummary { get; set; }
        public string? ResumeFileUrl { get; set; }
        public string? DigitalSignatureUrl { get; set; }

        // --- Geographic & Availability ---
        [MaxLength(100)] public string? Country { get; set; }
        [MaxLength(100)] public string? City { get; set; }
        [Column(TypeName = "varchar(50)")] public string? Timezone { get; set; }
        public DateTime? AvailableStartDate { get; set; }

        // --- Compliance (Legal Audit Trail) ---
        [Required]
        public bool TermsAndConditionsAccepted { get; set; }
        public DateTime? TermsAcceptedAt { get; set; }

        // --- Navigation ---
        // One-to-One: The Mastery Hub
        public virtual CandidateMastery Mastery { get; set; } = null!;

        // One-to-Many: Experience is separate for high-performance job matching
        public virtual ICollection<CandidateExperience> Experiences { get; set; } = new HashSet<CandidateExperience>();
    }
}