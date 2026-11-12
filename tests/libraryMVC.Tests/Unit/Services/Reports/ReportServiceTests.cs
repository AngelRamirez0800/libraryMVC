using ClosedXML.Excel;
using libraryMVC.DTOs.Reports;
using libraryMVC.Interfaces;
using libraryMVC.Services.Reports;
using Moq;
using Xunit;

namespace libraryMVC.Tests.Unit.Services.Reports;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task GenerateBooksPdfIncludesRelatedDataAndBuildsDownloadMetadata()
    {
        var repository = new Mock<IReportDataRepository>();
        var imageProvider = new Mock<IReportImageProvider>();
        imageProvider.Setup(provider => provider.ReadAsync("images/books/cover.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3]);
        repository.Setup(data => data.GetBooksAsync(
                It.IsAny<ReportFilters?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new BookReportData(
                    "978-1-234567-89-0",
                    "Example book",
                    "A book summary",
                    ["Ada Lovelace", "Grace Hopper"],
                    ["Science", "History"],
                    false,
                    [
                        new BookImageReportData("images/books/cover.png", "cover.png"),
                        new BookImageReportData("images/books/back.png", "back.png")
                    ])
            ]);
        TabularReport? capturedReport = null;
        var pdfGenerator = new Mock<IPdfReportGenerator>();
        pdfGenerator
            .Setup(generator => generator.Generate(It.IsAny<TabularReport>()))
            .Callback<TabularReport>(report => capturedReport = report)
            .Returns([1, 2, 3]);
        var excelGenerator = new Mock<IExcelReportGenerator>();
        var service = new ReportService(
            repository.Object,
            pdfGenerator.Object,
            excelGenerator.Object,
            imageProvider.Object);

        var filters = new ReportFilters(Search: "Example", AuthorId: Guid.NewGuid());
        ReportFile file = await service.GenerateAsync(
            ReportEntity.Books,
            ReportFormat.Pdf,
            TestContext.Current.CancellationToken,
            filters);

        Assert.Equal("application/pdf", file.ContentType);
        Assert.Matches(@"^books_\d{8}_\d{6}\.pdf$", file.FileName);
        Assert.Equal(new byte[] { 1, 2, 3 }, file.Content);
        Assert.NotNull(capturedReport);
        Assert.Equal("Books report", capturedReport.Title);
        Assert.Equal(
            ["ISBN", "Title", "Summary", "Authors", "Categories", "Image Preview", "Status", "Image Count"],
            capturedReport.Columns.Select(column => column.Header));
        Assert.Equal("Ada Lovelace, Grace Hopper", capturedReport.Rows[0][3]);
        Assert.Equal("Science, History", capturedReport.Rows[0][4]);
        Assert.Equal(string.Empty, capturedReport.Rows[0][5]);
        Assert.Equal("Inactive", capturedReport.Rows[0][6]);
        Assert.Equal("2", capturedReport.Rows[0][7]);
        ReportEmbeddedImage image = Assert.Single(capturedReport.Images!);
        Assert.Equal(0, image.RowIndex);
        Assert.Equal(5, image.ColumnIndex);
        Assert.Equal("cover.png", image.FileName);
        Assert.Equal(".png", image.ImageExtension);
        Assert.Equal(new byte[] { 1, 2, 3 }, image.Content);
        repository.Verify(data => data.GetBooksAsync(filters, It.IsAny<CancellationToken>()), Times.Once);
        excelGenerator.Verify(
            generator => generator.Generate(It.IsAny<TabularReport>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateCategoriesExcelUsesCategoryWorksheetAndExcelContentType()
    {
        var repository = new Mock<IReportDataRepository>();
        var filters = new ReportFilters(Search: "Science");
        repository.Setup(data => data.GetCategoriesAsync(
                It.IsAny<ReportFilters?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new CategoryReportData("Science", "Scientific works", true, ["Example book"])
            ]);
        var excelGenerator = new Mock<IExcelReportGenerator>();
        excelGenerator
            .Setup(generator => generator.Generate(It.IsAny<TabularReport>(), "Categories"))
            .Returns([4, 5, 6]);
        var service = new ReportService(
            repository.Object,
            Mock.Of<IPdfReportGenerator>(),
            excelGenerator.Object,
            Mock.Of<IReportImageProvider>());

        ReportFile file = await service.GenerateAsync(
            ReportEntity.Categories,
            ReportFormat.Excel,
            TestContext.Current.CancellationToken,
            filters);

        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            file.ContentType);
        Assert.Matches(@"^categories_\d{8}_\d{6}\.xlsx$", file.FileName);
        Assert.Equal(new byte[] { 4, 5, 6 }, file.Content);
        excelGenerator.Verify(
            generator => generator.Generate(
                It.Is<TabularReport>(report =>
                    report.Rows.Count == 1
                    && report.Rows[0][3] == "Example book"),
                "Categories"),
            Times.Once);
        repository.Verify(data => data.GetCategoriesAsync(filters, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateBooksExcelOmitsImageFilesAndRetainsImageCount()
    {
        var repository = new Mock<IReportDataRepository>();
        repository.Setup(data => data.GetBooksAsync(
                It.IsAny<ReportFilters?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new BookReportData(
                    "978-1-234567-89-0",
                    "Example book",
                    "A book summary",
                    [],
                    [],
                    true,
                    [new BookImageReportData("images/books/missing.jpg", "missing.jpg")])
            ]);
        var imageProvider = new Mock<IReportImageProvider>();
        imageProvider.Setup(provider => provider.ReadAsync(
                "images/books/missing.jpg",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        var service = new ReportService(
            repository.Object,
            Mock.Of<IPdfReportGenerator>(),
            new ExcelReportGenerator(),
            imageProvider.Object);

        ReportFile file = await service.GenerateAsync(
            ReportEntity.Books,
            ReportFormat.Excel,
            TestContext.Current.CancellationToken);

        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        IXLWorksheet worksheet = workbook.Worksheet("Books");
        Assert.Equal(
            ["ISBN", "Title", "Summary", "Authors", "Categories", "Image Preview", "Status", "Image Count"],
            Enumerable.Range(1, 8).Select(column => worksheet.Cell(4, column).GetString()));
        Assert.Equal("1", worksheet.Cell(5, 8).GetString());
        Assert.Equal(string.Empty, worksheet.Cell(4, 9).GetString());
    }

    [Fact]
    public async Task MissingBookImageIsExplicitlyLabeledInTheReport()
    {
        var repository = new Mock<IReportDataRepository>();
        repository.Setup(data => data.GetBooksAsync(
                It.IsAny<ReportFilters?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new BookReportData(
                    "978-1-234567-89-0",
                    "Example book",
                    "A book summary",
                    [],
                    [],
                    true,
                    [new BookImageReportData("images/books/missing.jpg", "missing.jpg")])
            ]);
        var imageProvider = new Mock<IReportImageProvider>();
        imageProvider.Setup(provider => provider.ReadAsync(
                "images/books/missing.jpg",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        TabularReport? capturedReport = null;
        var pdfGenerator = new Mock<IPdfReportGenerator>();
        pdfGenerator.Setup(generator => generator.Generate(It.IsAny<TabularReport>()))
            .Callback<TabularReport>(report => capturedReport = report)
            .Returns([1]);
        var service = new ReportService(
            repository.Object,
            pdfGenerator.Object,
            Mock.Of<IExcelReportGenerator>(),
            imageProvider.Object);

        await service.GenerateAsync(
            ReportEntity.Books,
            ReportFormat.Pdf,
            TestContext.Current.CancellationToken);

        Assert.Equal("Image unavailable", capturedReport?.Rows[0][5]);
        Assert.Empty(capturedReport?.Images ?? []);
    }
}
