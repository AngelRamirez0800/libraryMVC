using libraryMVC.DTOs;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace libraryMVC.Tests.Unit.Services;

public class BookApplicationServiceTests
{
    [Fact]
    public async Task CreateAsyncCreatesBookAndUpdatesSelectedRelationships()
    {
        var bookService = new Mock<IBooksService>();
        var authorService = new Mock<IBookAuthorService>();
        var categoryService = new Mock<ICategoryBookService>();
        var categoryQueries = new Mock<ICategoryQueries>();
        var imageService = new Mock<IBookImageService>();
        var service = new BookApplicationService(
            bookService.Object,
            authorService.Object,
            categoryService.Object,
            categoryQueries.Object,
            imageService.Object);
        var categoryId = Guid.NewGuid();
        var category = new Category("Fiction", string.Empty);
        SetId(category, categoryId);
        categoryQueries.Setup(queries => queries.GetAllActiveAsync())
            .ReturnsAsync(new List<Category?> { category });
        bookService.Setup(books => books.AddAsync(It.IsAny<Book>()))
            .Returns((Book book) => Task.FromResult(book));

        var authorIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var imageFiles = new List<IFormFile> { Mock.Of<IFormFile>() };
        var result = await service.CreateAsync(
            new BookInput { Isbn = "978-1-234567-89-0", Title = "Novel", Summary = "A story" },
            authorIds,
            new[] { categoryId, Guid.Empty, categoryId },
            imageFiles);

        Assert.Equal("Novel", result.Title);
        Assert.NotEqual(default, result.CreatedAt);
        bookService.Verify(books => books.AddAsync(It.Is<Book>(book => book.Id == result.Id)), Times.Once);
        authorService.Verify(authors => authors.UpdateAsync(
            result.Id,
            It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(authorIds))), Times.Once);
        categoryService.Verify(categories => categories.UpdateForBookAsync(
            result.Id,
            It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(new[] { categoryId }))), Times.Once);
        imageService.Verify(images => images.AddToBookAsync(result.Id, imageFiles), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncRejectsMissingCategorySelectionBeforeWriting()
    {
        var (service, books, authors, categories, categoryQueries, images) = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new BookInput(),
            null,
            new[] { Guid.Empty },
            null));

        categoryQueries.Verify(queries => queries.GetAllActiveAsync(), Times.Never);
        books.Verify(serviceDependency => serviceDependency.AddAsync(It.IsAny<Book>()), Times.Never);
        authors.Verify(serviceDependency => serviceDependency.UpdateAsync(
            It.IsAny<Guid>(),
            It.IsAny<IEnumerable<Guid>>()), Times.Never);
        categories.Verify(serviceDependency => serviceDependency.UpdateForBookAsync(
            It.IsAny<Guid>(),
            It.IsAny<IEnumerable<Guid>>()), Times.Never);
        images.Verify(serviceDependency => serviceDependency.AddToBookAsync(
            It.IsAny<Guid>(),
            It.IsAny<ICollection<IFormFile>>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsyncRejectsInactiveCategoryWithoutCreatingBook()
    {
        var (service, books, _, _, categoryQueries, _) = CreateService();
        var categoryId = Guid.NewGuid();
        categoryQueries.Setup(queries => queries.GetAllActiveAsync()).ReturnsAsync(new List<Category?>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new BookInput(),
            null,
            new[] { categoryId },
            null));

        categoryQueries.Verify(queries => queries.GetAllActiveAsync(), Times.Once);
        books.Verify(serviceDependency => serviceDependency.AddAsync(It.IsAny<Book>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncReturnsNullForMissingBookWithoutUpdatingRelationships()
    {
        var (service, books, authors, categories, _, images) = CreateService();
        var bookId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var categoryQueries = new Mock<ICategoryQueries>();
        categoryQueries.Setup(queries => queries.GetAllActiveAsync())
            .ReturnsAsync(new List<Category?> { CreateCategory(categoryId) });
        service = new BookApplicationService(
            books.Object,
            authors.Object,
            categories.Object,
            categoryQueries.Object,
            images.Object);
        books.Setup(bookService => bookService.GetByIdAsync(bookId)).ReturnsAsync((Book?)null);

        var result = await service.UpdateAsync(
            bookId,
            new BookInput(),
            Array.Empty<Guid>(),
            new[] { categoryId },
            null);

        Assert.Null(result);
        books.Verify(bookService => bookService.GetByIdAsync(bookId), Times.Once);
        books.Verify(bookService => bookService.UpdateAsync(It.IsAny<Book>()), Times.Never);
        authors.Verify(authorService => authorService.UpdateAsync(
            It.IsAny<Guid>(),
            It.IsAny<IEnumerable<Guid>>()), Times.Never);
        categories.Verify(categoryService => categoryService.UpdateForBookAsync(
            It.IsAny<Guid>(),
            It.IsAny<IEnumerable<Guid>>()), Times.Never);
        images.Verify(imageService => imageService.AddToBookAsync(
            It.IsAny<Guid>(),
            It.IsAny<ICollection<IFormFile>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncUpdatesBookAndSynchronizesRelationships()
    {
        var (service, books, authors, categories, categoryQueries, images) = CreateService();
        var bookId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        categoryQueries.Setup(queries => queries.GetAllActiveAsync())
            .ReturnsAsync(new List<Category?> { CreateCategory(categoryId) });
        var book = new Book("978-1-234567-89-0", "Old title", "Old summary");
        SetId(book, bookId);
        books.Setup(bookService => bookService.GetByIdAsync(bookId)).ReturnsAsync(book);
        var authorIds = new[] { Guid.NewGuid() };
        var imageFiles = new List<IFormFile> { Mock.Of<IFormFile>() };
        var result = await service.UpdateAsync(
            bookId,
            new BookInput { Isbn = "978-1-234567-89-1", Title = "New title", Summary = "New summary", IsActive = false },
            authorIds,
            new[] { categoryId },
            imageFiles);

        Assert.Same(book, result);
        Assert.Equal("New title", book.Title);
        Assert.False(book.IsActive);
        books.Verify(bookService => bookService.UpdateAsync(book), Times.Once);
        authors.Verify(authorService => authorService.UpdateAsync(
            bookId,
            It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(authorIds))), Times.Once);
        categories.Verify(categoryService => categoryService.UpdateForBookAsync(
            bookId,
            It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(new[] { categoryId }))), Times.Once);
        images.Verify(imageService => imageService.AddToBookAsync(bookId, imageFiles), Times.Once);
    }

    private static (BookApplicationService Service, Mock<IBooksService> Books, Mock<IBookAuthorService> Authors,
        Mock<ICategoryBookService> Categories, Mock<ICategoryQueries> CategoryQueries, Mock<IBookImageService> Images) CreateService()
    {
        var books = new Mock<IBooksService>();
        books.Setup(service => service.AddAsync(It.IsAny<Book>()))
            .Returns((Book book) => Task.FromResult(book));
        var authors = new Mock<IBookAuthorService>();
        var categories = new Mock<ICategoryBookService>();
        var categoryQueries = new Mock<ICategoryQueries>();
        var images = new Mock<IBookImageService>();
        return (new BookApplicationService(books.Object, authors.Object, categories.Object, categoryQueries.Object, images.Object),
            books, authors, categories, categoryQueries, images);
    }

    private static Category CreateCategory(Guid id)
    {
        var category = new Category("Fiction", string.Empty);
        SetId(category, id);
        return category;
    }

    private static void SetId(Entity<Guid> entity, Guid id) =>
        typeof(Entity<Guid>).GetProperty(nameof(Entity<Guid>.Id))!.SetValue(entity, id);
}
