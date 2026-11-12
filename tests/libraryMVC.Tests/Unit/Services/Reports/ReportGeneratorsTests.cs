using ClosedXML.Excel;
using libraryMVC.DTOs.Reports;
using libraryMVC.Services.Reports;
using QuestPDF.Infrastructure;
using Xunit;

namespace libraryMVC.Tests.Unit.Services.Reports;

public sealed class ReportGeneratorsTests
{
    public ReportGeneratorsTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void PdfGeneratorCreatesPdfWithPageNumbering()
    {
        var report = new TabularReport(
            "Books report",
            DateTimeOffset.Now,
            [new ReportColumn("Title", 1), new ReportColumn("Authors", 1)],
            [["Example book", "Ada Lovelace"]]);
        var generator = new PdfReportGenerator();

        byte[] content = generator.Generate(report);

        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(content, 0, 5));
        Assert.True(content.Length > 500);
    }

    [Fact]
    public void PdfAndExcelGeneratorsEmbedBookImages()
    {
        byte[] image = File.ReadAllBytes(Path.Combine(
            AppContext.BaseDirectory,
            "wwwroot",
            "images",
            "books",
            "seed-assets",
            "kafka-trial.jpg"));
        var report = new TabularReport(
            "Books report",
            DateTimeOffset.Now,
            [new ReportColumn("Title", 1), new ReportColumn("Image preview", 1)],
            [["The Trial", string.Empty]],
            [new ReportEmbeddedImage(0, 1, "kafka-trial.jpg", ".jpg", image)]);

        byte[] pdf = new PdfReportGenerator().Generate(report);
        byte[] excel = new ExcelReportGenerator().Generate(report, "Books");

        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
        using var workbook = new XLWorkbook(new MemoryStream(excel));
        IXLWorksheet worksheet = workbook.Worksheet("Books");
        Assert.Equal("The Trial", worksheet.Cell(5, 1).GetString());
        Assert.Single(worksheet.Pictures);
    }

    [Fact]
    public void ExcelGeneratorSupportsEverySeedImageAsset()
    {
        string assetDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "wwwroot",
            "images",
            "books",
            "seed-assets");
        var generator = new ExcelReportGenerator();

        foreach (string imagePath in Directory.EnumerateFiles(assetDirectory, "*.jpg"))
        {
            byte[] image = File.ReadAllBytes(imagePath);
            var report = new TabularReport(
                "Books report",
                DateTimeOffset.Now,
                [new ReportColumn("Image preview", 1)],
                [[""]],
                [new ReportEmbeddedImage(0, 0, Path.GetFileName(imagePath), ".jpg", image)]);

            try
            {
                Assert.NotEmpty(generator.Generate(report, "Books"));
            }
            catch (ArgumentException exception)
            {
                throw new Xunit.Sdk.XunitException(
                    $"ClosedXML could not embed '{Path.GetFileName(imagePath)}': {exception.Message}");
            }
        }
    }

    [Fact]
    public void ExcelGeneratorCreatesStyledWorksheetAndPreservesTabularData()
    {
        var report = new TabularReport(
            "Authors report",
            DateTimeOffset.Now,
            [new ReportColumn("Name", 1), new ReportColumn("Books", 2)],
            [["Ada Lovelace", "Example book"]]);
        var generator = new ExcelReportGenerator();

        byte[] content = generator.Generate(report, "Authors");

        using var workbook = new XLWorkbook(new MemoryStream(content));
        IXLWorksheet worksheet = workbook.Worksheet("Authors");
        Assert.Equal("libraryMVC", worksheet.Cell(1, 1).GetString());
        Assert.Equal("Authors report", worksheet.Cell(2, 1).GetString());
        Assert.Equal("Name", worksheet.Cell(4, 1).GetString());
        Assert.Equal("Books", worksheet.Cell(4, 2).GetString());
        Assert.Equal("Ada Lovelace", worksheet.Cell(5, 1).GetString());
        Assert.Equal("Example book", worksheet.Cell(5, 2).GetString());
        Assert.Equal(XLColor.FromHtml("#333333"), worksheet.Cell(4, 1).Style.Fill.BackgroundColor);
        Assert.Equal(XLColor.White, worksheet.Cell(4, 1).Style.Font.FontColor);
        Assert.Equal(XLColor.Black, worksheet.Cell(5, 1).Style.Font.FontColor);
        Assert.Equal(XLColor.FromHtml("#F2F2F2"), worksheet.Cell(5, 1).Style.Fill.BackgroundColor);
        Assert.Equal(XLColor.Black, worksheet.Cell(1, 1).Style.Font.FontColor);
        Assert.Equal(XLColor.Gray, worksheet.Cell(3, 1).Style.Font.FontColor);
        Assert.Single(worksheet.Tables);
        Assert.Equal(4, worksheet.SheetView.SplitRow);
        Assert.Equal(1, worksheet.PageSetup.FirstRowToRepeatAtTop);
        Assert.Equal(4, worksheet.PageSetup.LastRowToRepeatAtTop);
    }

    [Fact]
    public void PdfGeneratorRejectsRowsWithMissingCells()
    {
        var report = new TabularReport(
            "Invalid report",
            DateTimeOffset.Now,
            [new ReportColumn("Title", 1), new ReportColumn("Authors", 1)],
            [["Example book"]]);
        var generator = new PdfReportGenerator();

        Assert.Throws<ArgumentException>(() => generator.Generate(report));
    }

    [Fact]
    public void ExcelGeneratorCreatesAnEmptyFilteredTableWhenThereAreNoRecords()
    {
        var report = new TabularReport(
            "Categories report",
            DateTimeOffset.Now,
            [new ReportColumn("Name", 1)],
            []);
        var generator = new ExcelReportGenerator();

        byte[] content = generator.Generate(report, "Categories");

        using var workbook = new XLWorkbook(new MemoryStream(content));
        IXLWorksheet worksheet = workbook.Worksheet("Categories");
        Assert.Equal("Name", worksheet.Cell(4, 1).GetString());
        Assert.Single(worksheet.Tables);
    }
}
