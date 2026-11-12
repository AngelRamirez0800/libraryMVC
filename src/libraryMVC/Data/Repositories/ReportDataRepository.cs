using libraryMVC.DTOs.Reports;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Repositories;

public sealed class ReportDataRepository : IReportDataRepository
{
    private readonly ApplicationDbContext _context;

    public ReportDataRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BookReportData>> GetBooksAsync(
        ReportFilters? filters = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Book> query = _context.Books
            .AsNoTracking()
            .AsSplitQuery()
            .Include(book => book.Authors.OrderBy(author => author.LastName).ThenBy(author => author.FirstName))
            .Include(book => book.Categories.OrderBy(category => category.Name))
            .Include(book => book.Images.OrderBy(image => image.ImageNumber))
            .AsQueryable();

        string? search = Normalize(filters?.Search);
        if (search is not null)
        {
            query = query.Where(book => book.Title.Contains(search) || book.Isbn.Contains(search));
        }

        if (filters?.AuthorId is Guid authorId)
        {
            query = query.Where(book => book.Authors.Any(author => author.Id == authorId));
        }

        if (filters?.CategoryId is Guid categoryId)
        {
            query = query.Where(book => book.Categories.Any(category => category.Id == categoryId));
        }

        var books = await query.OrderBy(book => book.Title).ThenBy(book => book.Id).ToListAsync(cancellationToken);

        return books.Select(book => new BookReportData(
                book.Isbn,
                book.Title,
                book.Summary,
                book.Authors.Select(author => $"{author.FirstName} {author.LastName}").ToArray(),
                book.Categories.Select(category => category.Name).ToArray(),
                book.IsActive,
                book.Images.Select(image => new BookImageReportData(
                    image.ImagePath,
                    image.OriginalFileName)).ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyList<AuthorReportData>> GetAuthorsAsync(
        ReportFilters? filters = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Author> query = _context.Authors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(author => author.Books.OrderBy(book => book.Title))
            .AsQueryable();

        string? search = Normalize(filters?.Search);
        if (search is not null)
        {
            query = query.Where(author =>
                author.FirstName.Contains(search) || author.LastName.Contains(search));
        }

        string? nationality = Normalize(filters?.Nationality);
        if (nationality is not null)
        {
            query = query.Where(author => author.Nationality == nationality);
        }

        var authors = await query
            .OrderBy(author => author.LastName)
            .ThenBy(author => author.FirstName)
            .ThenBy(author => author.Id)
            .ToListAsync(cancellationToken);

        return authors.Select(author => new AuthorReportData(
                author.FirstName,
                author.LastName,
                author.Nationality,
                author.BirthDate,
                author.IsActive,
                author.Books.Select(book => book.Title).ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyList<CategoryReportData>> GetCategoriesAsync(
        ReportFilters? filters = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Category> query = _context.Categories
            .AsNoTracking()
            .AsSplitQuery()
            .Include(category => category.Books.OrderBy(book => book.Title))
            .AsQueryable();

        string? search = Normalize(filters?.Search);
        if (search is not null)
        {
            query = query.Where(category => category.Name.Contains(search));
        }

        var categories = await query
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .ToListAsync(cancellationToken);

        return categories.Select(category => new CategoryReportData(
                category.Name,
                category.Description,
                category.IsActive,
                category.Books.Select(book => book.Title).ToArray()))
            .ToArray();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
