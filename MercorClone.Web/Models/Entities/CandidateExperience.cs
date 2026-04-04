using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MercorClone.Web.Models.Entities
{
    [Index(nameof(CandidateProfileId))]
    [Index(nameof(JobTitle))] 
    public class CandidateExperience : BaseEntity
    {
        [Required]
        public Guid CandidateProfileId { get; set; }

        [ForeignKey(nameof(CandidateProfileId))]
        public virtual CandidateProfile CandidateProfile { get; set; } = null!;

        [Required, MaxLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        public bool IsCurrentRole { get; set; } = false;

        [Column(TypeName = "text")]
        public string? Description { get; set; }
        
        [MaxLength(100)]
        public string? Location { get; set; }
    }
}