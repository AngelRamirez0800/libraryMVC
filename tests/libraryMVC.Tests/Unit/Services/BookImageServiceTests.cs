using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace libraryMVC.Tests.Unit.Services;

public sealed class BookImageServiceTests
{
    [Fact]
    public async Task AddToBookAsyncRejectsAnEmptySelectionBeforeCallingDependencies()
    {
        var (service, storage, repository, validator) = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddToBookAsync(Guid.NewGuid(), Array.Empty<IFormFile>()));

        storage.Verify(value => value.SaveAsync(It.IsAny<Guid>(), It.IsAny<IFormFile>()), Times.Never);
        repository.Verify(value => value.AddAsync(It.IsAny<BookImage>()), Times.Never);
        validator.Verify(value => value.Validate(It.IsAny<IFormFile>()), Times.Never);
    }

    [Fact]
    public async Task AddToBookAsyncValidatesStoresAndPersistsImageMetadata()
    {
        var (service, storage, repository, validator) = CreateService();
        Guid bookId = Guid.NewGuid();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var imageFile = new FormFile(stream, 0, stream.Length, "image", "cover.png");
        storage.Setup(value => value.SaveAsync(bookId, imageFile))
            .ReturnsAsync(new StoredBookImage($"images/books/{bookId}/01.png", 1, stream.Length));

        await service.AddToBookAsync(bookId, new[] { imageFile });

        validator.Verify(value => value.Validate(imageFile), Times.Once);
        storage.Verify(value => value.SaveAsync(bookId, imageFile), Times.Once);
        repository.Verify(value => value.AddAsync(It.Is<BookImage>(image =>
            image.BookId == bookId
            && image.ImagePath == $"images/books/{bookId}/01.png"
            && image.ImageNumber == 1
            && image.OriginalFileName == "cover.png"
            && image.FileSize == stream.Length)), Times.Once);
    }

    [Fact]
    public async Task AddToBookAsyncDoesNotStoreAnImageRejectedByTheValidator()
    {
        var (service, storage, repository, validator) = CreateService();
        using var stream = new MemoryStream(new byte[] { 1 });
        var imageFile = new FormFile(stream, 0, stream.Length, "image", "cover.gif");
        validator.Setup(value => value.Validate(imageFile))
            .Throws(new ArgumentException("Unsupported image."));

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddToBookAsync(Guid.NewGuid(), new[] { imageFile }));

        storage.Verify(value => value.SaveAsync(It.IsAny<Guid>(), It.IsAny<IFormFile>()), Times.Never);
        repository.Verify(value => value.AddAsync(It.IsAny<BookImage>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAsyncDeletesThePersistedImageAndItsStoredFile()
    {
        var (service, storage, repository, _) = CreateService();
        Guid imageId = Guid.NewGuid();
        var image = new BookImage(Guid.NewGuid(), "images/books/book/01.png", 1, "cover.png", 3);
        repository.Setup(value => value.GetByIdAsync(imageId)).ReturnsAsync(image);

        await service.RemoveAsync(imageId);

        repository.Verify(value => value.RemoveAsync(image), Times.Once);
        storage.Verify(value => value.Delete(image), Times.Once);
    }

    [Fact]
    public async Task RemoveAsyncRejectsMissingImagesWithoutDeletingFiles()
    {
        var (service, storage, repository, _) = CreateService();
        repository.Setup(value => value.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BookImage?)null);
        Guid imageId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveAsync(imageId));

        repository.Verify(value => value.RemoveAsync(It.IsAny<BookImage>()), Times.Never);
        storage.Verify(value => value.Delete(It.IsAny<BookImage>()), Times.Never);
    }

    private static (
        BookImageService Service,
        Mock<IBookImageStorage> Storage,
        Mock<IBookImageRepository> Repository,
        Mock<IImageFileValidator> Validator) CreateService()
    {
        var storage = new Mock<IBookImageStorage>();
        var repository = new Mock<IBookImageRepository>();
        var validator = new Mock<IImageFileValidator>();
        return (new BookImageService(storage.Object, repository.Object, validator.Object), storage, repository, validator);
    }
}
