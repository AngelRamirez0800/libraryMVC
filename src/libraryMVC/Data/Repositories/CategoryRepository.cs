using libraryMVC.DTOs.Lists;
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

        public async Task<PageSlice<CategoryListItem>> GetPageAsync(
            CategoryListFilter filters,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filters);
            int normalizedPageSize = Math.Clamp(pageSize, 1, 100);
            int maxPage = int.MaxValue / normalizedPageSize;
            int normalizedPage = Math.Clamp(page, 1, maxPage);
            string? search = string.IsNullOrWhiteSpace(filters.Search) ? null : filters.Search.Trim();

            IQueryable<Category> query = _context.Categories.AsNoTracking();
            if (search is not null)
            {
                query = query.Where(category => category.Name.Contains(search));
            }

            List<CategoryListItem> rows = await query
                .OrderBy(category => category.Name)
                .ThenBy(category => category.Id)
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize + 1)
                .Select(category => new CategoryListItem(
                    category.Id,
                    category.Name,
                    category.Description,
                    category.Books.Count,
                    category.IsActive))
                .ToListAsync(cancellationToken);

            bool hasMore = rows.Count > normalizedPageSize;
            if (hasMore)
            {
                rows.RemoveAt(rows.Count - 1);
            }

            return new PageSlice<CategoryListItem>(rows, normalizedPage, normalizedPageSize, hasMore);
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
