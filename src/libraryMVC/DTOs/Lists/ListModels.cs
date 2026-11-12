namespace libraryMVC.DTOs.Lists;

public static class ListPageDefaults
{
    public const int PageSize = 15;
}

public sealed class AuthorListFilter
{
    public string? Search
    {
        get; set;
    }
    public string? Nationality
    {
        get; set;
    }
}

public sealed class BookListFilter
{
    public string? Search
    {
        get; set;
    }
    public Guid? CategoryId
    {
        get; set;
    }
    public Guid? AuthorId
    {
        get; set;
    }
}

public sealed class CategoryListFilter
{
    public string? Search
    {
        get; set;
    }
}

public sealed record PageSlice<T>(IReadOnlyList<T> Items, int Page, int PageSize, bool HasMore);

public sealed record FilterOption(Guid Id, string Name);

public sealed record BookFilterOptions(
    IReadOnlyList<FilterOption> Authors,
    IReadOnlyList<FilterOption> Categories);

public sealed record AuthorListItem(
    Guid Id,
    string FirstName,
    string LastName,
    string Nationality,
    DateTime BirthDate,
    DateTime? UpdatedAt,
    bool IsActive);

public sealed record BookListItem(
    Guid Id,
    string Isbn,
    string Title,
    string Summary,
    IReadOnlyList<string> Categories,
    DateTime? UpdatedAt,
    bool IsActive);

public sealed record CategoryListItem(
    Guid Id,
    string Name,
    string Description,
    int BookCount,
    bool IsActive);

public sealed record AuthorsIndexViewModel(
    PageSlice<AuthorListItem> Results,
    AuthorListFilter Filters,
    IReadOnlyList<string> Nationalities);

public sealed record BooksIndexViewModel(
    PageSlice<BookListItem> Results,
    BookListFilter Filters,
    BookFilterOptions Options);

public sealed record CategoriesIndexViewModel(
    PageSlice<CategoryListItem> Results,
    CategoryListFilter Filters);
