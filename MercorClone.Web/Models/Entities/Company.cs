using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MercorClone.Web.Models.Entities
{
    [Index(nameof(Name))]
    [Index(nameof(Industry))]
    public class Company : BaseEntity
    {
        [Required]
        [Column(TypeName = "varchar(100)")]
        public string Name { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
        public string? LogoUrl { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? Industry { get; set; }
        public string? CompanyDescription { get; set; }
        public string? OfficeAddress { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SocialLinks { get; set; }
        [MaxLength(100)]
        public string? StripeCustomerId { get; set; } // The master Stripe ID for the 
        
        public NpgsqlTypes.NpgsqlTsVector? SearchVector { get; set; }
        public virtual CompanySubscription? Subscription { get; set; }
        public virtual ICollection<EmployerProfile> Employers { get; set; } = new HashSet<EmployerProfile>();
        public virtual ICollection<JobPost> JobPosts { get; set; } = new HashSet<JobPost>();
        public virtual ICollection<BillingTransaction> BillingTransactions { get; set; } = new HashSet<BillingTransaction>();
    }
    
}