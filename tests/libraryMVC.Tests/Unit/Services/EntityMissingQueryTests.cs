using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using Moq;
using Xunit;

namespace libraryMVC.Tests.Unit.Services;

public sealed class EntityMissingQueryTests
{
    [Fact]
    public async Task BooksServiceReturnsNullWhenBookIsNotFound()
    {
        var repository = new Mock<IBooksRepository>();
        Guid bookId = Guid.NewGuid();
        repository.Setup(value => value.GetByIdWithDetailsAsync(bookId)).ReturnsAsync((Book?)null);
        var service = new BooksService(repository.Object);

        Assert.Null(await service.GetByIdWithDetailsAsync(bookId));

        repository.Verify(value => value.GetByIdWithDetailsAsync(bookId), Times.Once);
    }

    [Fact]
    public async Task BooksServiceReturnsLifecycleResultForSoftDelete()
    {
        var repository = new Mock<IBooksRepository>();
        Guid bookId = Guid.NewGuid();
        repository.Setup(value => value.DeleteAsync(bookId)).ReturnsAsync(true);
        var service = new BooksService(repository.Object);

        Assert.True(await service.DeleteAsync(bookId));

        repository.Verify(value => value.DeleteAsync(bookId), Times.Once);

        repository.Setup(value => value.DeleteAsync(bookId)).ReturnsAsync(false);

        Assert.False(await service.DeleteAsync(bookId));

        repository.Verify(value => value.DeleteAsync(bookId), Times.Exactly(2));
    }

    [Fact]
    public async Task AuthorsServiceReturnsNullWhenAuthorIsNotFound()
    {
        var repository = new Mock<IAuthorsRepository>();
        Guid authorId = Guid.NewGuid();
        repository.Setup(value => value.GetByIdWithBooksAsync(authorId)).ReturnsAsync((Author?)null);
        var service = new AuthorsService(repository.Object, Mock.Of<IAuthorBookService>());

        Assert.Null(await service.GetByIdWithBooksAsync(authorId));

        repository.Verify(value => value.GetByIdWithBooksAsync(authorId), Times.Once);
    }

    [Fact]
    public async Task CategoriesServiceReturnsNullWhenCategoryIsNotFound()
    {
        var repository = new Mock<ICategoryRespository>();
        Guid categoryId = Guid.NewGuid();
        repository.Setup(value => value.GetByIdWithBooksAsync(categoryId)).ReturnsAsync((Category?)null);
        var service = new CategoryService(repository.Object, Mock.Of<ICategoryBookService>());

        Assert.Null(await service.GetByIdWithBooksAsync(categoryId));

        repository.Verify(value => value.GetByIdWithBooksAsync(categoryId), Times.Once);
    }
}
