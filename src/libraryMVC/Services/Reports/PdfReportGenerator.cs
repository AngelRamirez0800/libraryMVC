using libraryMVC.DTOs.Reports;
using libraryMVC.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace libraryMVC.Services.Reports;

public sealed class PdfReportGenerator : IPdfReportGenerator
{
    private const string HeaderColor = "#333333";
    private const string AlternateRowColor = "#f2f2f2";
    private const string BorderColor = "#b7b7b7";

    public byte[] Generate(TabularReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        ValidateRows(report);
        Dictionary<(int Row, int Column), ReportEmbeddedImage> images = (report.Images ?? [])
            .ToDictionary(image => (image.RowIndex, image.ColumnIndex));

        return Document.Create(document =>
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Black));

                page.Header().Column(column =>
                {
                    column.Item()
                        .Text("libraryMVC")
                        .FontSize(10)
                        .SemiBold()
                        .FontColor(Colors.Black);
                    column.Item()
                        .Text(report.Title)
                        .FontSize(18)
                        .Bold()
                        .FontColor(Colors.Black);
                    column.Item()
                        .PaddingBottom(12)
                        .Text($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss zzz}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken2);
                });

                page.Content().Column(column =>
                {
                    if (report.Rows.Count == 0)
                    {
                        column.Item()
                            .PaddingTop(16)
                            .Text("This report contains no records.")
                            .Italic()
                            .FontColor(Colors.Grey.Darken2);
                    }
                    else
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (ReportColumn reportColumn in report.Columns)
                                {
                                    columns.RelativeColumn(reportColumn.WidthWeight);
                                }
                            });

                            table.Header(header =>
                            {
                                foreach (ReportColumn reportColumn in report.Columns)
                                {
                                    header.Cell()
                                        .Element(HeaderCell)
                                        .Text(reportColumn.Header)
                                        .SemiBold()
                                        .FontColor(Colors.White);
                                }
                            });

                            for (int rowIndex = 0; rowIndex < report.Rows.Count; rowIndex++)
                            {
                                for (int columnIndex = 0; columnIndex < report.Columns.Count; columnIndex++)
                                {
                                    IContainer cell = table.Cell().Element(
                                        container => BodyCell(container, rowIndex));
                                    if (images.TryGetValue((rowIndex, columnIndex), out ReportEmbeddedImage? image))
                                    {
                                        cell.Height(48)
                                            .AlignCenter()
                                            .AlignMiddle()
                                            .Image(image.Content)
                                            .FitArea();
                                    }
                                    else
                                    {
                                        string value = report.Rows[rowIndex][columnIndex];
                                        cell.Text(string.IsNullOrWhiteSpace(value) ? "-" : value);
                                    }
                                }
                            }
                        });
                    }
                });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            }))
            .GeneratePdf();
    }

    private static void ValidateRows(TabularReport report)
    {
        if (report.Columns.Count == 0)
        {
            throw new ArgumentException("A report must define at least one column.", nameof(report));
        }

        if (report.Rows.Any(row => row.Count != report.Columns.Count))
        {
            throw new ArgumentException("Each report row must contain one value per column.", nameof(report));
        }

        if (report.Images?.Any(image =>
                image.RowIndex < 0 ||
                image.RowIndex >= report.Rows.Count ||
                image.ColumnIndex < 0 ||
                image.ColumnIndex >= report.Columns.Count ||
                image.Content.Length == 0) == true)
        {
            throw new ArgumentException("Embedded images must reference a valid non-empty report cell.", nameof(report));
        }
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Background(HeaderColor)
            .Border(0.5f)
            .BorderColor(BorderColor)
            .PaddingVertical(5)
            .PaddingHorizontal(4);

    private static IContainer BodyCell(IContainer container, int rowIndex) =>
        container
            .Background(rowIndex % 2 == 0 ? Colors.White : AlternateRowColor)
            .BorderBottom(0.5f)
            .BorderColor(BorderColor)
            .PaddingVertical(4)
            .PaddingHorizontal(4);
}
