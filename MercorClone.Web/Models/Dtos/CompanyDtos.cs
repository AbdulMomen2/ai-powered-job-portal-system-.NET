using System.ComponentModel.DataAnnotations;

namespace MercorClone.Web.Models.Dtos
{
    public class CompanyProfileDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
        public string? LogoUrl { get; set; }
        public string? Industry { get; set; }
        public string? CompanyDescription { get; set; }
        public string? OfficeAddress { get; set; }
        public string? SocialLinks { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }

    public class UpdateCompanyRequest
    {[Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;[Url] public string? WebsiteUrl { get; set; }
        [Url] public string? LogoUrl { get; set; }
        [MaxLength(50)] public string? Industry { get; set; }
        public string? CompanyDescription { get; set; }
        public string? OfficeAddress { get; set; }
        public string? SocialLinks { get; set; }
    }

    public class PublicJobListingDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Country { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime? PublishedAt { get; set; }
        public string[]? RequiredSkills { get; set; }
    }

    public class ReviewDto
    {
        public Guid Id { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsVerifiedEmployee { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateReviewRequest
    {[Required, Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }
        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;
        [Required, MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }

    public class PagedResponse<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(PageSize, 1));
    }

    // Enums for clean repository return states
    public enum RepoOperationStatus { Success, NotFound, Forbidden, Conflict }
}