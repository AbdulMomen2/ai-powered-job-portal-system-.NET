using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MercorClone.Web.Models.Entities
{
    [Index(nameof(CompanyId))][Index(nameof(StripeInvoiceId), IsUnique = true)]
    public class BillingTransaction : BaseEntity
    {
        [Required]
        public Guid CompanyId { get; set; }[ForeignKey(nameof(CompanyId))]
        public virtual Company Company { get; set; } = null!;[Required, MaxLength(100)]
        public string StripeInvoiceId { get; set; } = string.Empty;

        public decimal AmountTotal { get; set; }
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string Currency { get; set; } = "USD";

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Open"; // Open, Paid, Void, Uncollectible

        public string? HostedInvoiceUrl { get; set; } // Link to Stripe's printable PDF invoice
        public string? InvoicePdfUrl { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}