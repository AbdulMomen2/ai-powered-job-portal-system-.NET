using MercorClone.Web.Models.Dtos;

namespace MercorClone.Web.Repositories
{
    public interface ICompanyRepository
    {
        Task<CompanyProfileDto?> GetCompanyProfileAsync(Guid companyId, CancellationToken cancellationToken = default);
        
        Task<RepoOperationStatus> UpdateCompanyAsync(Guid companyId, Guid userId, UpdateCompanyRequest request, CancellationToken cancellationToken = default);
        
        Task<PagedResponse<PublicJobListingDto>> GetCompanyJobsAsync(Guid companyId, int page, int pageSize, CancellationToken cancellationToken = default);
        
        Task<PagedResponse<ReviewDto>> GetCompanyReviewsAsync(Guid companyId, int page, int pageSize, CancellationToken cancellationToken = default);
        
        Task<RepoOperationStatus> SubmitReviewAsync(Guid companyId, Guid reviewerId, CreateReviewRequest request, CancellationToken cancellationToken = default);
    }
}