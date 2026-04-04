using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using MercorClone.Web.Data;
using MercorClone.Web.Repositories.Interfaces;

namespace MercorClone.Web.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        internal DbSet<T> dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            this.dbSet = _context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid id) => await dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync() => await dbSet.ToListAsync();

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) 
            => await dbSet.Where(predicate).ToListAsync();

        public async Task AddAsync(T entity) => await dbSet.AddAsync(entity);

        public async Task AddRangeAsync(IEnumerable<T> entities) => await dbSet.AddRangeAsync(entities);

        public void Update(T entity) => dbSet.Update(entity);

        // Note: Because we use Global Query Filters for Soft Deletes in AppDbContext,
        // calling Remove will actually delete it from DB. 
        // For soft delete, you usually Update the entity's IsDeleted flag and call Update().
        public void Remove(T entity) => dbSet.Remove(entity);

        public void RemoveRange(IEnumerable<T> entities) => dbSet.RemoveRange(entities);
    }
}