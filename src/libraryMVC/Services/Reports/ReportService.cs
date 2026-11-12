using libraryMVC.DTOs.Reports;
using libraryMVC.Interfaces;

namespace libraryMVC.Services.Reports;

public sealed class ReportService : IReportService
{
    private const string PdfContentType = "application/pdf";
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IReportDataRepository _repository;
    private readonly IPdfReportGenerator _pdfGenerator;
    private readonly IExcelReportGenerator _excelGenerator;
    private readonly IReportImageProvider _imageProvider;

    public ReportService(
        IReportDataRepository repository,
        IPdfReportGenerator pdfGenerator,
        IExcelReportGenerator excelGenerator,
        IReportImageProvider imageProvider)
    {
        _repository = repository;
        _pdfGenerator = pdfGenerator;
        _excelGenerator = excelGenerator;
        _imageProvider = imageProvider;
    }

    public async Task<ReportFile> GenerateAsync(
        ReportEntity entity,
        ReportFormat format,
        CancellationToken cancellationToken = default,
        ReportFilters? filters = null)
    {
        DateTimeOffset generatedAt = DateTimeOffset.Now;
        TabularReport report = await CreateReportAsync(entity, generatedAt, filters, cancellationToken);
        string extension = format switch
        {
            ReportFormat.Pdf => "pdf",
            ReportFormat.Excel => "xlsx",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
        string contentType = format == ReportFormat.Pdf ? PdfContentType : ExcelContentType;
        byte[] content = format switch
        {
            ReportFormat.Pdf => _pdfGenerator.Generate(report),
            ReportFormat.Excel => _excelGenerator.Generate(report, GetWorksheetName(entity)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report format.")
        };
        string fileName = $"{GetFileName(entity)}_{generatedAt:yyyyMMdd_HHmmss}.{extension}";

        return new ReportFile(fileName, contentType, content);
    }

    private async Task<TabularReport> CreateReportAsync(
        ReportEntity entity,
        DateTimeOffset generatedAt,
        ReportFilters? filters,
        CancellationToken cancellationToken)
    {
        return entity switch
        {
            ReportEntity.Books => await CreateBooksReportAsync(
                await _repository.GetBooksAsync(filters, cancellationToken),
                generatedAt,
                cancellationToken),
            ReportEntity.Authors => CreateAuthorsReport(
                await _repository.GetAuthorsAsync(filters, cancellationToken),
                generatedAt),
            ReportEntity.Categories => CreateCategoriesReport(
                await _repository.GetCategoriesAsync(filters, cancellationToken),
                generatedAt),
            _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unsupported report entity.")
        };
    }

    private async Task<TabularReport> CreateBooksReportAsync(
        IReadOnlyList<BookReportData> books,
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken)
    {
        ReportColumn[] columns =
        [
            new("ISBN", 1.1f),
            new("Title", 1.5f),
            new("Summary", 2.2f),
            new("Authors", 1.4f),
            new("Categories", 1.3f),
            new("Image Preview", 0.9f),
            new("Status", 0.7f),
            new("Image Count", 0.8f)
        ];
        var rows = new List<IReadOnlyList<string>>(books.Count);
        var images = new List<ReportEmbeddedImage>();
        for (int rowIndex = 0; rowIndex < books.Count; rowIndex++)
        {
            BookReportData book = books[rowIndex];
            string imageLabel = book.Images.Count == 0 ? "No image" : "Image unavailable";
            foreach (BookImageReportData image in book.Images)
            {
                byte[]? content = await _imageProvider.ReadAsync(image.ImagePath, cancellationToken);
                if (content is null)
                {
                    continue;
                }

                imageLabel = string.Empty;
                images.Add(new ReportEmbeddedImage(
                    rowIndex,
                    5,
                    image.OriginalFileName,
                    Path.GetExtension(image.ImagePath),
                    content));
                break;
            }

            rows.Add((IReadOnlyList<string>)
            [
                book.Isbn,
                book.Title,
                book.Summary,
                JoinValues(book.Authors),
                JoinValues(book.Categories),
                imageLabel,
                GetStatus(book.IsActive),
                book.Images.Count.ToString()
            ]);
        }

        return new TabularReport("Books report", generatedAt, columns, rows, images);
    }

    private static TabularReport CreateAuthorsReport(
        IReadOnlyList<AuthorReportData> authors,
        DateTimeOffset generatedAt)
    {
        ReportColumn[] columns =
        [
            new("Name", 1.5f),
            new("Nationality", 1.1f),
            new("Birth date", 1f),
            new("Status", 0.7f),
            new("Books", 2.2f)
        ];
        IReadOnlyList<IReadOnlyList<string>> rows = authors
            .Select(author => (IReadOnlyList<string>)
            [
                $"{author.FirstName} {author.LastName}",
                author.Nationality,
                author.BirthDate.ToString("yyyy-MM-dd"),
                GetStatus(author.IsActive),
                JoinValues(author.BookTitles)
            ])
            .ToArray();

        return new TabularReport("Authors report", generatedAt, columns, rows);
    }

    private static TabularReport CreateCategoriesReport(
        IReadOnlyList<CategoryReportData> categories,
        DateTimeOffset generatedAt)
    {
        ReportColumn[] columns =
        [
            new("Name", 1.2f),
            new("Description", 2.2f),
            new("Status", 0.7f),
            new("Books", 2.2f)
        ];
        IReadOnlyList<IReadOnlyList<string>> rows = categories
            .Select(category => (IReadOnlyList<string>)
            [
                category.Name,
                category.Description,
                GetStatus(category.IsActive),
                JoinValues(category.BookTitles)
            ])
            .ToArray();

        return new TabularReport("Categories report", generatedAt, columns, rows);
    }

    private static string JoinValues(IEnumerable<string> values) =>
        string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string GetStatus(bool isActive) => isActive ? "Active" : "Inactive";

    private static string GetFileName(ReportEntity entity) => entity switch
    {
        ReportEntity.Books => "books",
        ReportEntity.Authors => "authors",
        ReportEntity.Categories => "categories",
        _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unsupported report entity.")
    };

    private static string GetWorksheetName(ReportEntity entity) => entity switch
    {
        ReportEntity.Books => "Books",
        ReportEntity.Authors => "Authors",
        ReportEntity.Categories => "Categories",
        _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unsupported report entity.")
    };
}
