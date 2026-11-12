namespace libraryMVC.DTOs.Reports;

public enum ReportEntity
{
    Books,
    Authors,
    Categories
}

public enum ReportFormat
{
    Pdf,
    Excel
}

public sealed record ReportColumn(string Header, float WidthWeight);

public sealed record TabularReport(
    string Title,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<ReportEmbeddedImage>? Images = null);

public sealed record ReportEmbeddedImage(
    int RowIndex,
    int ColumnIndex,
    string FileName,
    string ImageExtension,
    byte[] Content);

public sealed record ReportFile(string FileName, string ContentType, byte[] Content);

public sealed record BookReportData(
    string Isbn,
    string Title,
    string Summary,
    IReadOnlyList<string> Authors,
    IReadOnlyList<string> Categories,
    bool IsActive,
    IReadOnlyList<BookImageReportData> Images);

public sealed record BookImageReportData(string ImagePath, string OriginalFileName);

public sealed record AuthorReportData(
    string FirstName,
    string LastName,
    string Nationality,
    DateTime BirthDate,
    bool IsActive,
    IReadOnlyList<string> BookTitles);

public sealed record CategoryReportData(
    string Name,
    string Description,
    bool IsActive,
    IReadOnlyList<string> BookTitles);
