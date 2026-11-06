using libraryMVC.Data;
using libraryMVC.Models;
using libraryMVC.Tests.Support.Builders;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Tests.Integration.Infrastructure;

internal static class LibraryDatabaseSeeder
{
    private const string ImagesDirectory = "images/books";

    private static readonly byte[] OnePixelPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0B, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0xA5, 0xF6, 0x45, 0x40, 0x00,
        0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    ];

    public static async Task SeedAsync(
        ApplicationDbContext context,
        string webRootPath,
        CancellationToken cancellationToken = default)
    {
        TestLibraryData data = TestLibraryData.Create();

        context.Authors.AddRange(data.Authors);
        context.Categories.AddRange(data.Categories);
        context.Books.AddRange(data.Books);
        await context.SaveChangesAsync(cancellationToken);

        List<BookImage> images = new(data.Books.Count);
        foreach (Book book in data.Books)
        {
            string relativeDirectory = Path.Combine(ImagesDirectory, book.Id.ToString());
            string absoluteDirectory = Path.Combine(webRootPath, relativeDirectory);
            Directory.CreateDirectory(absoluteDirectory);

            const string fileName = "01.png";
            await File.WriteAllBytesAsync(
                Path.Combine(absoluteDirectory, fileName),
                OnePixelPng,
                cancellationToken);

            string imagePath = $"{ImagesDirectory}/{book.Id}/{fileName}";
            var image = new BookImage(book.Id, imagePath, 1, fileName, OnePixelPng.Length);
            book.AddImage(image);
            images.Add(image);
        }

        context.BookImages.AddRange(images);
        await context.SaveChangesAsync(cancellationToken);

        await VerifySeedAsync(context, cancellationToken);
    }

    private static async Task VerifySeedAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        int authors = await context.Authors.CountAsync(cancellationToken);
        int books = await context.Books.CountAsync(cancellationToken);
        int categories = await context.Categories.CountAsync(cancellationToken);
        int images = await context.BookImages.CountAsync(cancellationToken);

        if (authors != 2 || books != 10 || categories != 5 || images != 10)
        {
            throw new InvalidOperationException(
                $"Test seed integrity check failed: expected 2 authors, 10 books, 5 categories and 10 images; " +
                $"found {authors}, {books}, {categories} and {images}.");
        }
    }
}
