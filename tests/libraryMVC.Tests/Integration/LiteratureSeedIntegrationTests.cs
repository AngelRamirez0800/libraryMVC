using ClosedXML.Excel;
using libraryMVC.Data;
using libraryMVC.Data.Seeding;
using libraryMVC.DTOs.Reports;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace libraryMVC.Tests.Integration;

public sealed class LiteratureSeedIntegrationTests : IntegrationTestBase
{
    private readonly IntegrationDatabaseFixture _fixture;

    public LiteratureSeedIntegrationTests(IntegrationDatabaseFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LiteratureSeedIsIdempotentAndProducesCompleteReportData()
    {
        LiteratureSeedResult firstRun;
        await using (var firstScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            ILiteratureSeedService seed = firstScope.ServiceProvider
                .GetRequiredService<ILiteratureSeedService>();
            firstRun = await seed.SeedAsync(TestContext.Current.CancellationToken);
        }

        string firstSeedIsbn = LiteratureSeedCatalog.Authors
            .SelectMany(author => author.Books)
            .First(book => book.ImageFileName is not null)
            .Isbn;
        string targetImagePath;
        await using (var mutationScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            ApplicationDbContext mutationContext = mutationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            BookImage firstImage = await mutationContext.BookImages
                .Include(image => image.Book)
                .SingleAsync(
                    image => image.Book.Isbn == firstSeedIsbn,
                    TestContext.Current.CancellationToken);
            targetImagePath = Path.Combine(
                _fixture.WebRootPath,
                firstImage.ImagePath.Replace('/', Path.DirectorySeparatorChar));
        }

        await File.AppendAllTextAsync(
            targetImagePath,
            "stale",
            TestContext.Current.CancellationToken);

        LiteratureSeedResult secondRun;
        await using (var secondScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            ILiteratureSeedService seed = secondScope.ServiceProvider
                .GetRequiredService<ILiteratureSeedService>();
            secondRun = await seed.SeedAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(new LiteratureSeedResult(3, 21, 27, 15), firstRun);
        Assert.Equal(firstRun, secondRun);

        await using var validationScope = _fixture.Factory.Services.CreateAsyncScope();
        IServiceProvider services = validationScope.ServiceProvider;
        ApplicationDbContext context = services.GetRequiredService<ApplicationDbContext>();
        string[] isbns = LiteratureSeedCatalog.Authors
            .SelectMany(author => author.Books)
            .Select(book => book.Isbn)
            .ToArray();
        List<Book> books = await context.Books
            .Where(book => isbns.Contains(book.Isbn))
            .Include(book => book.Authors)
            .Include(book => book.Categories)
            .Include(book => book.Images)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(21, books.Count);
        Assert.Equal(21, books.Select(book => book.Isbn).Distinct(StringComparer.Ordinal).Count());
        Assert.All(books, book =>
        {
            Assert.NotEqual(Guid.Empty, book.Id);
            Assert.False(context.Entry(book).Property(entity => entity.Id).IsTemporary);
            Assert.Single(book.Authors);
            Assert.NotEmpty(book.Categories);
        });

        List<Author> authors = await context.Authors
            .Where(author => LiteratureSeedCatalog.Authors
                .Select(seedAuthor => seedAuthor.FirstName)
                .Contains(author.FirstName))
            .Include(author => author.Books)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, authors.Count);
        Assert.All(authors, author =>
            Assert.Equal(7, author.Books.Count(book => isbns.Contains(book.Isbn))));

        List<BookImage> seededImages = await context.BookImages
            .Where(image => isbns.Contains(image.Book.Isbn))
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(15, seededImages.Count);
        foreach (BookImage image in seededImages)
        {
            string imagePath = Path.Combine(
                _fixture.WebRootPath,
                image.ImagePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(imagePath), $"Missing image file: {image.ImagePath}");
            Assert.Equal(new FileInfo(imagePath).Length, image.FileSize);
            Assert.NotEqual(Guid.Empty, image.Id);
            Assert.False(context.Entry(image).Property(entity => entity.Id).IsTemporary);
        }
        BookImage refreshedImage = Assert.Single(books.Single(book => book.Isbn == firstSeedIsbn).Images);
        string sourceImagePath = Path.Combine(
            AppContext.BaseDirectory,
            "wwwroot",
            "images",
            "books",
            "seed-assets",
            LiteratureSeedCatalog.Authors
                .SelectMany(author => author.Books)
                .Single(book => book.Isbn == firstSeedIsbn)
                .ImageFileName!);
        Assert.Equal(
            await File.ReadAllBytesAsync(sourceImagePath, TestContext.Current.CancellationToken),
            await File.ReadAllBytesAsync(targetImagePath, TestContext.Current.CancellationToken));
        Assert.Equal(new FileInfo(targetImagePath).Length, refreshedImage.FileSize);

        IReportService reportService = services.GetRequiredService<IReportService>();
        foreach (ReportEntity entity in Enum.GetValues<ReportEntity>())
        {
            foreach (ReportFormat format in Enum.GetValues<ReportFormat>())
            {
                ReportFile file = await reportService.GenerateAsync(
                    entity,
                    format,
                    TestContext.Current.CancellationToken);
                Assert.True(file.Content.Length > 0);
                Assert.Matches(@"^\w+_\d{8}_\d{6}\.", file.FileName);
                Assert.Equal(
                    format == ReportFormat.Pdf ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    file.ContentType);
                if (format == ReportFormat.Pdf)
                {
                    Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(file.Content, 0, 5));
                }
                else
                {
                    Assert.Equal((byte)'P', file.Content[0]);
                    Assert.Equal((byte)'K', file.Content[1]);
                    if (entity == ReportEntity.Books)
                    {
                        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
                        IXLWorksheet worksheet = workbook.Worksheet("Books");
                        Assert.Equal(31, worksheet.Tables.Single().DataRange.RowCount());
                        Assert.Equal(25, worksheet.Pictures.Count);
                    }
                }
            }
        }
    }
}
