using libraryMVC.Abstractions;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace libraryMVC.Data.Repositories
{
    public class Repository<T, I> :
        IEntityReaderRepository<T, I>,
        IEntityWriterRepository<T, I>,
        IEntityLifecycleRepository<T, I>
        where T : Entity<I>
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<bool> DeleteAsync(I id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null || !entity.IsActive)
                return false;

            entity.Deactivate();
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public virtual async Task<bool> RestoreAsync(I id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null || entity.IsActive)
                return false;

            entity.Activate();
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public virtual async Task<bool> AlreadyExistsAsync(I id)
        {
            var entity = await _dbSet.FindAsync(id);
            return entity != null;
        }

        public virtual async Task<T?> GetByIdAsync(I id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null) return entity;
            return await _dbSet.FirstOrDefaultAsync(e => EF.Property<I>(e, "Id")!.Equals(id));
        }

        public virtual async Task<IList<T?>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IList<T?>> GetAllActiveAsync()
        {
            return await _dbSet.Where(e => e.IsActive).ToListAsync();
        }

        public virtual async Task<IList<T?>> GetAllInactiveAsync()
        {
            return await _dbSet.Where(e => !e.IsActive).ToListAsync();
        }
    }
}