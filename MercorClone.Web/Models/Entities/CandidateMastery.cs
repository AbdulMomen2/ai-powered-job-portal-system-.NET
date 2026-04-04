using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace MercorClone.Web.Models.Entities
{
    [Index(nameof(CandidateProfileId), IsUnique = true)]
    public class CandidateMastery : BaseEntity
    {
        [Required]
        public Guid CandidateProfileId { get; set; }

        [ForeignKey(nameof(CandidateProfileId))]
        public virtual CandidateProfile CandidateProfile { get; set; } = null!;

        // --- The "Superdupper Fast" Search Engine ---
        // Native Postgres Array for sub-millisecond skill filtering
        public string[] SkillTags { get; set; } = Array.Empty<string>();

        // --- Achievements & Proof (JSONB: Display-heavy, No Joins needed) ---
        [Column(TypeName = "jsonb")]
        public string? Educations { get; set; } // [{ "school": "AIUB", "degree": "BSc", "end": "2026" }]

        [Column(TypeName = "jsonb")]
        public string? Projects { get; set; } // [{ "name": "Surma AI", "url": "...", "desc": "..." }]

        [Column(TypeName = "jsonb")]
        public string? Certifications { get; set; } // [{ "name": "AWS", "id": "123" }]

        [Column(TypeName = "jsonb")]
        public string? Publications { get; set; } 

        [Column(TypeName = "jsonb")]
        public string? Awards { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SocialLinks { get; set; } // { "github": "...", "linkedin": "..." }

        // --- High-Level Metrics for AI ---
        public int TotalYearsOfExperience { get; set; }
        public string? TopSkillsSummary { get; set; } 
    }
}