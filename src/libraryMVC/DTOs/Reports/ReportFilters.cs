namespace libraryMVC.DTOs.Reports;

public sealed record ReportFilters(
    string? Search = null,
    string? Nationality = null,
    Guid? AuthorId = null,
    Guid? CategoryId = null);
