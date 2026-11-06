using libraryMVC.Data.Storage;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace libraryMVC.Tests.Unit.Storage;

public sealed class LocalBookImageStorageTests : IAsyncLifetime
{
    private readonly string _webRootPath = Path.Combine(Path.GetTempPath(), $"library-tests-{Guid.NewGuid():N}");
    private LocalBookImageStorage _storage = null!;

    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_webRootPath);
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.WebRootPath).Returns(_webRootPath);
        _storage = new LocalBookImageStorage(environment.Object);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_webRootPath))
        {
            Directory.Delete(_webRootPath, recursive: true);
        }

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task SaveAsyncStoresImageAndReturnsItsMetadata()
    {
        Guid bookId = Guid.NewGuid();
        byte[] content = [1, 2, 3, 4];
        using var input = new MemoryStream(content);
        var image = new FormFile(input, 0, content.Length, "image", "cover.JPG");

        StoredBookImage result = await _storage.SaveAsync(bookId, image);

        Assert.Equal($"images/books/{bookId}/01.jpg", result.ImagePath);
        Assert.Equal(1, result.ImageNumber);
        Assert.Equal(content.Length, result.FileSize);
        Assert.Equal(
            content,
            await File.ReadAllBytesAsync(
                Path.Combine(_webRootPath, result.ImagePath),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsyncAssignsDistinctNamesWhenWritesRace()
    {
        Guid bookId = Guid.NewGuid();
        MemoryStream[] inputs = Enumerable.Range(0, 20)
            .Select(_ => new MemoryStream([1, 2, 3]))
            .ToArray();
        Task<StoredBookImage>[] writes = inputs
            .Select(input =>
            {
                var image = new FormFile(input, 0, input.Length, "image", "cover.png");
                return _storage.SaveAsync(bookId, image);
            })
            .ToArray();

        StoredBookImage[] results;
        try
        {
            results = await Task.WhenAll(writes);
        }
        finally
        {
            foreach (MemoryStream input in inputs)
            {
                input.Dispose();
            }
        }

        Assert.Equal(20, results.Select(result => result.ImageNumber).Distinct().Count());
        Assert.Equal(20, Directory.GetFiles(Path.Combine(_webRootPath, "images", "books", bookId.ToString())).Length);
        foreach (StoredBookImage result in results)
        {
            await using FileStream stream = new(
                Path.Combine(_webRootPath, result.ImagePath),
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);
            Assert.Equal(3, stream.Length);
        }
    }

    [Fact]
    public async Task DeleteRemovesImageAndEmptyBookDirectory()
    {
        Guid bookId = Guid.NewGuid();
        byte[] content = [1, 2, 3];
        using var input = new MemoryStream(content);
        StoredBookImage storedImage = await _storage.SaveAsync(
            bookId,
            new FormFile(input, 0, content.Length, "image", "cover.webp"));
        var image = new BookImage(bookId, storedImage.ImagePath, storedImage.ImageNumber, "cover.webp", storedImage.FileSize);
        string imageDirectory = Path.Combine(_webRootPath, "images", "books", bookId.ToString());

        _storage.Delete(image);

        Assert.False(File.Exists(Path.Combine(_webRootPath, storedImage.ImagePath)));
        Assert.False(Directory.Exists(imageDirectory));
    }

    [Fact]
    public async Task SaveAsyncUsesWebpExtensionWhenFileNameHasNoExtension()
    {
        byte[] content = [1];
        using var input = new MemoryStream(content);
        var image = new FormFile(input, 0, content.Length, "image", "cover");

        StoredBookImage result = await _storage.SaveAsync(Guid.NewGuid(), image);

        Assert.EndsWith(".webp", result.ImagePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsyncRemovesPartialFileWhenCopyFails()
    {
        Guid bookId = Guid.NewGuid();
        var image = new Mock<IFormFile>();
        image.SetupGet(value => value.FileName).Returns("cover.png");
        image.Setup(value => value.CopyToAsync(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .Returns((Stream destination, CancellationToken _) => WriteThenFailAsync(destination));

        await Assert.ThrowsAsync<IOException>(() => _storage.SaveAsync(bookId, image.Object));

        string imageDirectory = Path.Combine(_webRootPath, "images", "books", bookId.ToString());
        Assert.Empty(Directory.GetFiles(imageDirectory));
    }

    private static async Task WriteThenFailAsync(Stream destination)
    {
        await destination.WriteAsync(new byte[] { 1, 2, 3 });
        throw new IOException("Simulated copy failure.");
    }
}
