using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MercorClone.Web.Models.Entities
{
    [Index(nameof(UserId), nameof(CompanyId), IsUnique = true)]
    public class EmployerProfile : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        public Guid CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public virtual Company Company { get; set; } = null!;

        [Required]
        [Column(TypeName = "varchar(100)")] 
        public string JobTitle { get; set; } = string.Empty; 

        // 2. Metadata for auditing/permissions
        public bool IsPrimaryContact { get; set; } = false;
    }
}