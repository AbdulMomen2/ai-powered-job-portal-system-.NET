namespace MercorClone.Web.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Add specific repositories here as you need them later
        // IJobPostRepository JobPosts { get; }
        // ICandidateProfileRepository Candidates { get; }
        
        Task<int> CompleteAsync();
    }
}