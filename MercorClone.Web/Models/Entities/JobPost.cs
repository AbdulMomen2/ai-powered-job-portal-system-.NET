using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MercorClone.Web.Models.Entities
{
    [Index(nameof(CompanyId))]
    [Index(nameof(Status))]
    [Index(nameof(Category))]
    [Index(nameof(Title))] 
    public class JobPost : BaseEntity
    {
        [Required]
        public Guid CompanyId { get; set; }
        
        [ForeignKey(nameof(CompanyId))]
        public virtual Company Company { get; set; } = null!;

        [Required]
        public Guid PostedByUserId { get; set; }
        
        [ForeignKey(nameof(PostedByUserId))]
        public virtual ApplicationUser PostedByUser { get; set; } = null!;

        // --- Core Job Info ---
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty; // e.g., "Software Engineering"

        [MaxLength(50)]
        public string ContractType { get; set; } = string.Empty; // Full-time, Part-time

        [MaxLength(50)]
        public string LocationType { get; set; } = string.Empty; // Remote, Onsite, Hybrid
        
        public string? Location { get; set; } // Specific city/country if not fully remote

        // --- Financials & Logistics ---
        public string WorkingHours { get; set; } = string.Empty;
        
        // Enterprise Optimization: Use Min/Max for better filtering
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string? Country { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        [Column(TypeName = "varchar(10)")]
        public string Currency { get; set; } = "USD";

        public int HiresTarget { get; set; } // Number of people needed for this role

        [Column(TypeName = "text")] // Optimized for large HTML content
        public string JobDescriptionHtml { get; set; } = string.Empty;

        // --- The Matching Engine (PostgreSQL Specifics) ---
        // Native array for sub-millisecond skill matching (@> operator)
        public string[]? RequiredSkills { get; set; } 
        
        [Column(TypeName = "jsonb")]
        public string? AdditionalRequirements { get; set; } // Languages, specific tools, etc.

        // Status as Enum-like string for performance
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; 

        // Audit for Enterprise
        public DateTime? PublishedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        // --- Navigation ---
        public virtual ICollection<JobApplication> Applications { get; set; } = new HashSet<JobApplication>();
    }
}