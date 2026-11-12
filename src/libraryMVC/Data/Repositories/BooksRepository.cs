using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using libraryMVC.DTOs.Lists;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Repositories
{
    public class BooksRepository : Repository<Book, Guid>, IBooksRepository
    {
        private readonly ApplicationDbContext _context;

        public BooksRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Book?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Categories)
                .Include(b => b.Images.OrderBy(i => i.ImageNumber))
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public override async Task<IList<Book?>> GetAllAsync()
        {
            return await _context.Books
            .Include(book => book.Authors)
            .Include(book => book.Categories)
                .Cast<Book?>()
                .ToListAsync();
        }

        public async Task<PageSlice<BookListItem>> GetPageAsync(
            BookListFilter filters,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filters);
            (page, pageSize) = NormalizePage(page, pageSize);

            IQueryable<Book> query = _context.Books.AsNoTracking();
            string? search = Normalize(filters.Search);
            if (search is not null)
            {
                query = query.Where(book => book.Title.Contains(search) || book.Isbn.Contains(search));
            }

            if (filters.AuthorId.HasValue)
            {
                Guid authorId = filters.AuthorId.Value;
                query = query.Where(book => book.Authors.Any(author => author.Id == authorId));
            }

            if (filters.CategoryId.HasValue)
            {
                Guid categoryId = filters.CategoryId.Value;
                query = query.Where(book => book.Categories.Any(category => category.Id == categoryId));
            }

            List<BookListItem> rows = await query
                .OrderBy(book => book.Title)
                .ThenBy(book => book.Isbn)
                .ThenBy(book => book.Id)
                .Skip(GetSkip(page, pageSize))
                .Take(pageSize + 1)
                .Select(book => new BookListItem(
                    book.Id,
                    book.Isbn,
                    book.Title,
                    book.Summary,
                    book.Categories
                        .OrderBy(category => category.Name)
                        .Select(category => category.Name)
                        .ToArray(),
                    book.UpdatedAt,
                    book.IsActive))
                .ToListAsync(cancellationToken);

            bool hasMore = rows.Count > pageSize;
            if (hasMore)
            {
                rows.RemoveAt(rows.Count - 1);
            }

            return new PageSlice<BookListItem>(rows, page, pageSize, hasMore);
        }

        public async Task<BookFilterOptions> GetFilterOptionsAsync(
            CancellationToken cancellationToken = default)
        {
            FilterOption[] authors = await _context.Authors
                .AsNoTracking()
                .OrderBy(author => author.LastName)
                .ThenBy(author => author.FirstName)
                .Select(author => new FilterOption(author.Id, author.FirstName + " " + author.LastName))
                .ToArrayAsync(cancellationToken);
            FilterOption[] categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.Name)
                .Select(category => new FilterOption(category.Id, category.Name))
                .ToArrayAsync(cancellationToken);

            return new BookFilterOptions(authors, categories);
        }

        public async Task<List<Book>> GetRandomFeaturedBooksAsync(int count = 3)
        {
            var activeBooks = await _context.Books
                .Where(b => b.IsActive)
                .Include(b => b.Authors)
                .Include(b => b.Categories)
                .Include(b => b.Images.OrderBy(i => i.ImageNumber))
                .ToListAsync();

            return activeBooks
                .OrderBy(_ => Random.Shared.Next())
                .Take(count)
                .ToList();
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static (int Page, int PageSize) NormalizePage(int page, int pageSize)
        {
            int normalizedPageSize = Math.Clamp(pageSize, 1, 100);
            int maxPage = int.MaxValue / normalizedPageSize;
            return (Math.Clamp(page, 1, maxPage), normalizedPageSize);
        }

        private static int GetSkip(int page, int pageSize) => (page - 1) * pageSize;
    }
}
