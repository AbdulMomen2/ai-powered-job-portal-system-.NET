using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MercorClone.Web.Models.Entities
{
    // 1. PERFORMANCE: Composite index for fast recruiter filtering
    [Index(nameof(JobPostId), nameof(Status))] 
    // 2. INTEGRITY: Prevent duplicate applications (One candidate per job)
    [Index(nameof(JobPostId), nameof(CandidateProfileId), IsUnique = true)]
    // 3. LATENCY: Fast sorting by AI Match Score
    [Index(nameof(AiMatchScore))]
    public class JobApplication : BaseEntity
    {
        [Required]
        public Guid JobPostId { get; set; }
        
        [ForeignKey(nameof(JobPostId))]
        public virtual JobPost JobPost { get; set; } = null!;

        [Required]
        public Guid CandidateProfileId { get; set; }
        
        [ForeignKey(nameof(CandidateProfileId))]
        public virtual CandidateProfile CandidateProfile { get; set; } = null!;

        // --- Workflow Status ---
        [Required, MaxLength(30)]
        public string Status { get; set; } = "Applied"; 

        // --- AI ENGINE FIELDS (Matching) ---
        // Precision 5, Scale 2 allows scores like 99.99% with minimal storage
        [Column(TypeName = "decimal(5,2)")]
        public decimal? AiMatchScore { get; set; }
        
        [Column(TypeName = "jsonb")]
        public string? AiResumeAnalysisJson { get; set; } 

        // --- AI Interview Pipeline ---
        public DateTime? AiInterviewScheduledAt { get; set; }
        
        [Column(TypeName = "varchar(512)")]
        public string? AiInterviewVideoUrl { get; set; }
        
        [Column(TypeName = "jsonb")]
        public string? AiInterviewTranscriptJson { get; set; } 

        // --- Enterprise Feedback Loop ---
        public string? RejectionReason { get; set; } // Why the AI or Human rejected them
        
        [Column(TypeName = "jsonb")]
        public string? RecruiterNotes { get; set; } // Internal comments for the hiring team
        
        public DateTime? LastStatusUpdate { get; set; }
    }
}