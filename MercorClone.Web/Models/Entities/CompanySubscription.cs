using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MercorClone.Web.Models.Entities
{[Index(nameof(CompanyId), IsUnique = true)] // One active subscription per company
    [Index(nameof(StripeSubscriptionId))]
    public class CompanySubscription : BaseEntity
    {
        [Required]
        public Guid CompanyId { get; set; }[ForeignKey(nameof(CompanyId))]
        public virtual Company Company { get; set; } = null!;[Required]
        public Guid SubscriptionPlanId { get; set; }[ForeignKey(nameof(SubscriptionPlanId))]
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        // --- Stripe Core Fields ---
        [MaxLength(100)]
        public string? StripeSubscriptionId { get; set; }
        
        [MaxLength(100)]
        public string? StripeCustomerId { get; set; }

        // --- Subscription State ---
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Trialing"; // Active, PastDue, Canceled, Trialing

        public DateTime? CurrentPeriodStart { get; set; }
        public DateTime? CurrentPeriodEnd { get; set; }
        public bool CancelAtPeriodEnd { get; set; } = false;

        // --- Usage Tracking (Reset Monthly) ---
        public int AiInterviewsUsedThisMonth { get; set; } = 0;
    }
}