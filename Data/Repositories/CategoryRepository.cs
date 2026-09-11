using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Repositories
{
    public class CategoryRepository : Repository<Category, Guid>, ICategoryRespository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Category?> GetByIdWithBooksAsync(Guid id)
        {
            return await _context.Categories
                .Include(category => category.Books)
                .FirstOrDefaultAsync(category => category.Id == id);
        }

        public override async Task<IList<Category?>> GetAllAsync()
        {
            return await _context.Categories
                .Include(category => category.Books)
                .ToListAsync();
        }

        public Task<bool> HasBooksAsync(Guid id)
        {
            return _context.Categories
                .Where(category => category.Id == id)
                .SelectMany(category => category.Books)
                .AnyAsync();
        }

        public Task<bool> NameExistsAsync(string name, Guid? excludingId = null)
        {
            var normalizedName = name.Trim().ToLower();
            return _context.Categories.AnyAsync(category =>
                category.Name.ToLower() == normalizedName &&
                (!excludingId.HasValue || category.Id != excludingId.Value));
        }
    }
}
