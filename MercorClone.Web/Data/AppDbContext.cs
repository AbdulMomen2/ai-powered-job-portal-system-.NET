using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MercorClone.Web.Models.Entities;

namespace MercorClone.Web.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Core DbSets
        public DbSet<Company> Companies { get; set; }
        public DbSet<EmployerProfile> EmployerProfiles { get; set; }
        public DbSet<CandidateProfile> CandidateProfiles { get; set; }
        public DbSet<CandidateMastery> CandidateMasteries { get; set; }
        public DbSet<CandidateExperience> CandidateExperiences { get; set; }
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }

        public DbSet<CompanyReview> CompanyReviews { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<CompanySubscription> CompanySubscriptions { get; set; }
        public DbSet<BillingTransaction> BillingTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); 

            // 1. GLOBAL QUERY FILTERS (Soft Delete Implementation)
            builder.Entity<ApplicationUser>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Company>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<CandidateProfile>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<CandidateExperience>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<JobPost>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<JobApplication>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<EmployerProfile>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<CandidateMastery>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<CompanySubscription>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<BillingTransaction>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<CompanyReview>().HasQueryFilter(x => !x.IsDeleted);


            // 2. RELATIONSHIPS & CASCADE DELETES
            // Safely protect Job Posts from being deleted if a Recruiter leaves the company
            builder.Entity<JobPost>()
                .HasOne(j => j.PostedByUser)
                .WithMany()
                .HasForeignKey(j => j.PostedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Protect Job Applications if a Candidate deletes their account (Keeps HR records intact)
            builder.Entity<JobApplication>()
                .HasOne(ja => ja.CandidateProfile)
                .WithMany()
                .HasForeignKey(ja => ja.CandidateProfileId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.Entity<JobApplication>()
                .HasOne(ja => ja.JobPost)
                .WithMany(j => j.Applications)
                .HasForeignKey(ja => ja.JobPostId)
                .OnDelete(DeleteBehavior.Restrict);

            // Link Mastery 1-to-1 strictly
            builder.Entity<CandidateProfile>()
                .HasOne(c => c.Mastery)
                .WithOne(m => m.CandidateProfile)
                .HasForeignKey<CandidateMastery>(m => m.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting profile deletes mastery

            // Billing constraints
            builder.Entity<CompanySubscription>()
                .HasOne(cs => cs.Company)
                .WithOne(c => c.Subscription)
                .HasForeignKey<CompanySubscription>(cs => cs.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}