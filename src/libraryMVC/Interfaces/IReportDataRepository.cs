using libraryMVC.DTOs.Reports;

namespace libraryMVC.Interfaces;

public interface IReportDataRepository
{
    Task<IReadOnlyList<BookReportData>> GetBooksAsync(
        ReportFilters? filters = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuthorReportData>> GetAuthorsAsync(
        ReportFilters? filters = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryReportData>> GetCategoriesAsync(
        ReportFilters? filters = null,
        CancellationToken cancellationToken = default);
}
