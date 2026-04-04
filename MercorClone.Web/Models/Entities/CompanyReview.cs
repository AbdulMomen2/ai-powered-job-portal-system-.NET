using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MercorClone.Web.Models.Entities
{
    // Indexes for fast lookups by company
    // Prevents one user from reviewing the same company multiple times
    [Index(nameof(CompanyId))]
    [Index(nameof(ReviewerId), nameof(CompanyId), IsUnique = true)]
    public class CompanyReview : BaseEntity
    {
        [Required]
        public Guid CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public virtual Company Company { get; set; } = null!;

        [Required]
        public Guid ReviewerId { get; set; }

        [ForeignKey(nameof(ReviewerId))]
        public virtual ApplicationUser Reviewer { get; set; } = null!;

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        // Represents if the system verified they actually worked there
        public bool IsVerifiedEmployee { get; set; } = false;
    }
}
