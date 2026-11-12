using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using libraryMVC.DTOs.Lists;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Repositories
{
    public class AuthorsRepository : Repository<Author, Guid>, IAuthorsRepository
    {
        private readonly ApplicationDbContext _context;

        public AuthorsRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Author?> GetByIdWithBooksAsync(Guid id)
        {
            return await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<PageSlice<AuthorListItem>> GetPageAsync(
            AuthorListFilter filters,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filters);
            (page, pageSize) = NormalizePage(page, pageSize);

            IQueryable<Author> query = _context.Authors.AsNoTracking();
            string? search = Normalize(filters.Search);
            string? nationality = Normalize(filters.Nationality);
            if (search is not null)
            {
                query = query.Where(author =>
                    author.FirstName.Contains(search) ||
                    author.LastName.Contains(search) ||
                    (author.FirstName + " " + author.LastName).Contains(search));
            }

            if (nationality is not null)
            {
                query = query.Where(author => author.Nationality == nationality);
            }

            List<AuthorListItem> rows = await query
                .OrderBy(author => author.LastName)
                .ThenBy(author => author.FirstName)
                .ThenBy(author => author.Id)
                .Skip(GetSkip(page, pageSize))
                .Take(pageSize + 1)
                .Select(author => new AuthorListItem(
                    author.Id,
                    author.FirstName,
                    author.LastName,
                    author.Nationality,
                    author.BirthDate,
                    author.UpdatedAt,
                    author.IsActive))
                .ToListAsync(cancellationToken);

            bool hasMore = rows.Count > pageSize;
            if (hasMore)
            {
                rows.RemoveAt(rows.Count - 1);
            }

            return new PageSlice<AuthorListItem>(rows, page, pageSize, hasMore);
        }

        public async Task<IReadOnlyList<string>> GetNationalitiesAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.Authors
                .AsNoTracking()
                .Where(author => author.Nationality != string.Empty)
                .Select(author => author.Nationality)
                .Distinct()
                .OrderBy(nationality => nationality)
                .ToArrayAsync(cancellationToken);
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
