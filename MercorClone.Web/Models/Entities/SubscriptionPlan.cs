using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MercorClone.Web.Models.Entities
{[Index(nameof(StripePriceId), IsUnique = true)]
    public class SubscriptionPlan : BaseEntity
    {[Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "Pro AI Plan"

        [Required, MaxLength(100)]
        public string StripePriceId { get; set; } = string.Empty;

        public decimal MonthlyPrice { get; set; }
        
        [Column(TypeName = "varchar(10)")]
        public string Currency { get; set; } = "USD";

        // --- Usage Limits ---
        public int MaxActiveJobs { get; set; } // e.g., 5 active jobs at a time
        public int IncludedAiInterviews { get; set; } // e.g., 50 AI interviews/month[Column(TypeName = "jsonb")]
        public string? FeaturesListJson { get; set; } // For displaying on the pricing page

        public bool IsActive { get; set; } = true;
    }
}