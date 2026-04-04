using Microsoft.EntityFrameworkCore;
using MercorClone.Web.Data;
using MercorClone.Web.Models.Entities;
using MercorClone.Web.Models.Dtos;

namespace MercorClone.Web.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly AppDbContext _context;

        public CompanyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CompanyProfileDto?> GetCompanyProfileAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            return await _context.Companies
                .AsNoTracking()
                .Where(c => c.Id == companyId && !c.IsDeleted)
                .Select(c => new CompanyProfileDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    WebsiteUrl = c.WebsiteUrl,
                    LogoUrl = c.LogoUrl,
                    Industry = c.Industry,
                    CompanyDescription = c.CompanyDescription,
                    OfficeAddress = c.OfficeAddress,
                    SocialLinks = c.SocialLinks,
                    TotalReviews = _context.CompanyReviews.Count(r => r.CompanyId == companyId && !r.IsDeleted),
                    AverageRating = _context.CompanyReviews
                        .Where(r => r.CompanyId == companyId && !r.IsDeleted)
                        .Average(r => (double?)r.Rating) ?? 0.0
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<RepoOperationStatus> UpdateCompanyAsync(Guid companyId, Guid userId, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
        {
            // 1. Ensure user is actually an employer for this specific company
            var hasAccess = await _context.EmployerProfiles
                .AsNoTracking()
                .AnyAsync(e => e.UserId == userId && e.CompanyId == companyId && !e.IsDeleted, cancellationToken);

            if (!hasAccess) return RepoOperationStatus.Forbidden;

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId && !c.IsDeleted, cancellationToken);
            if (company == null) return RepoOperationStatus.NotFound;

            // 2. Map updates
            company.Name = request.Name;
            company.WebsiteUrl = request.WebsiteUrl;
            company.LogoUrl = request.LogoUrl;
            company.Industry = request.Industry;
            company.CompanyDescription = request.CompanyDescription;
            company.OfficeAddress = request.OfficeAddress;
            company.SocialLinks = request.SocialLinks;
            company.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return RepoOperationStatus.Success;
        }

        public async Task<PagedResponse<PublicJobListingDto>> GetCompanyJobsAsync(Guid companyId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.JobPosts
                .AsNoTracking()
                .Where(j => j.CompanyId == companyId && j.Status == "Active" && !j.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);
            var jobs = await query
                .OrderByDescending(j => j.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new PublicJobListingDto
                {
                    Id = j.Id,
                    Title = j.Title,
                    Category = j.Category,
                    LocationType = j.LocationType,
                    Location = j.Location,
                    Country = j.Country,
                    MinSalary = j.MinSalary,
                    MaxSalary = j.MaxSalary,
                    Currency = j.Currency,
                    PublishedAt = j.PublishedAt,
                    RequiredSkills = j.RequiredSkills
                })
                .ToListAsync(cancellationToken);

            return new PagedResponse<PublicJobListingDto>
            {
                Data = jobs, TotalCount = totalCount, Page = page, PageSize = pageSize
            };
        }

        public async Task<PagedResponse<ReviewDto>> GetCompanyReviewsAsync(Guid companyId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.CompanyReviews
                .AsNoTracking()
                .Where(r => r.CompanyId == companyId && !r.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    // Mask name for privacy (e.g. "John Doe" -> "J*** D.")
                    ReviewerName = string.IsNullOrEmpty(r.Reviewer.FullName) 
                        ? "Anonymous" 
                        : r.Reviewer.FullName.Substring(0, 1) + "*** " + r.Reviewer.FullName.Substring(r.Reviewer.FullName.LastIndexOf(' ') + 1, 1) + ".",
                    Rating = r.Rating,
                    Title = r.Title,
                    Content = r.Content,
                    IsVerifiedEmployee = r.IsVerifiedEmployee,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResponse<ReviewDto>
            {
                Data = reviews, TotalCount = totalCount, Page = page, PageSize = pageSize
            };
        }

        public async Task<RepoOperationStatus> SubmitReviewAsync(Guid companyId, Guid reviewerId, CreateReviewRequest request, CancellationToken cancellationToken = default)
        {
            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId && !c.IsDeleted, cancellationToken);
            if (!companyExists) return RepoOperationStatus.NotFound;

            var alreadyReviewed = await _context.CompanyReviews
                .AnyAsync(r => r.CompanyId == companyId && r.ReviewerId == reviewerId && !r.IsDeleted, cancellationToken);
            
            if (alreadyReviewed) return RepoOperationStatus.Conflict;

            // Auto-verify if they were ever hired by this company
            bool isVerified = await _context.JobApplications
                .AnyAsync(a => a.CandidateProfile.UserId == reviewerId 
                            && a.JobPost.CompanyId == companyId 
                            && a.Status == "Hired" && !a.IsDeleted, cancellationToken);

            var review = new CompanyReview
            {
                CompanyId = companyId,
                ReviewerId = reviewerId,
                Rating = request.Rating,
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                IsVerifiedEmployee = isVerified
            };

            _context.CompanyReviews.Add(review);
            await _context.SaveChangesAsync(cancellationToken);
            return RepoOperationStatus.Success;
        }
    }
}