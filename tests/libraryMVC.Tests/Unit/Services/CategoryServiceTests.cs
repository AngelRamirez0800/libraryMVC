using libraryMVC.DTOs;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using Moq;
using Xunit;

namespace libraryMVC.Tests.Unit.Services;

public class CategoryServiceTests
{
    [Fact]
    public async Task CreateAsyncTrimsDetailsAndSynchronizesBooks()
    {
        var repository = new Mock<ICategoryRespository>();
        var bookService = new Mock<ICategoryBookService>();
        repository.Setup(value => value.AddAsync(It.IsAny<Category>()))
            .Returns((Category category) => Task.FromResult(category));
        repository.Setup(value => value.NameExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>())).ReturnsAsync(false);
        var service = new CategoryService(repository.Object, bookService.Object);
        var bookIds = new[] { Guid.NewGuid() };

        var result = await service.CreateAsync(
            new CategoryInput { Name = "  Fiction  ", Description = "  Stories  " },
            bookIds);

        Assert.Equal("Fiction", result.Name);
        Assert.Equal("Stories", result.Description);
        Assert.True(result.IsActive);
        repository.Verify(value => value.AddAsync(It.Is<Category>(category => category.Name == "Fiction")), Times.Once);
        bookService.Verify(value => value.UpdateAsync(
            result.Id,
            It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(bookIds))), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncRejectsDuplicateNameBeforePersisting()
    {
        var repository = new Mock<ICategoryRespository>();
        var bookService = new Mock<ICategoryBookService>();
        repository.Setup(value => value.NameExistsAsync("Fiction", It.IsAny<Guid?>())).ReturnsAsync(true);
        var service = new CategoryService(repository.Object, bookService.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new CategoryInput { Name = "Fiction", Description = "Stories" },
            Array.Empty<Guid>()));

        repository.Verify(value => value.AddAsync(It.IsAny<Category>()), Times.Never);
        bookService.Verify(value => value.UpdateAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncReturnsNullForMissingCategoryWithoutCheckingNameOrBooks()
    {
        var repository = new Mock<ICategoryRespository>();
        var bookService = new Mock<ICategoryBookService>();
        var categoryId = Guid.NewGuid();
        repository.Setup(value => value.GetByIdAsync(categoryId)).ReturnsAsync((Category?)null);
        var service = new CategoryService(repository.Object, bookService.Object);

        var result = await service.UpdateAsync(categoryId, new CategoryInput(), Array.Empty<Guid>());

        Assert.Null(result);
        repository.Verify(value => value.GetByIdAsync(categoryId), Times.Once);
        repository.Verify(value => value.NameExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
        bookService.Verify(value => value.UpdateAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncTrimsDetailsAndReplacesBookAssociations()
    {
        var repository = new Mock<ICategoryRespository>();
        var bookService = new Mock<ICategoryBookService>();
        Guid categoryId = Guid.NewGuid();
        var category = new Category("Old name", "Old description");
        SetId(category, categoryId);
        repository.Setup(value => value.GetByIdAsync(categoryId)).ReturnsAsync(category);
        repository.Setup(value => value.NameExistsAsync(" New name ", categoryId)).ReturnsAsync(false);
        repository.Setup(value => value.UpdateAsync(It.IsAny<Category>()))
            .Returns((Category updatedCategory) => Task.FromResult(updatedCategory));
        var service = new CategoryService(repository.Object, bookService.Object);
        Guid[] bookIds = [Guid.NewGuid()];

        Category? result = await service.UpdateAsync(
            categoryId,
            new CategoryInput { Name = " New name ", Description = " New description ", IsActive = false },
            bookIds);

        Assert.Same(category, result);
        Assert.Equal("New name", category.Name);
        Assert.Equal("New description", category.Description);
        Assert.False(category.IsActive);
        repository.Verify(value => value.UpdateAsync(category), Times.Once);
        bookService.Verify(value => value.UpdateAsync(categoryId, It.Is<IEnumerable<Guid>>(
            ids => ids.SequenceEqual(bookIds))), Times.Once);
    }

    [Fact]
    public async Task UpdateAsyncRejectsDuplicateNameWithoutChangingCategory()
    {
        var repository = new Mock<ICategoryRespository>();
        var bookService = new Mock<ICategoryBookService>();
        Guid categoryId = Guid.NewGuid();
        var category = new Category("Existing", "Description");
        repository.Setup(value => value.GetByIdAsync(categoryId)).ReturnsAsync(category);
        repository.Setup(value => value.NameExistsAsync("Duplicate", categoryId)).ReturnsAsync(true);
        var service = new CategoryService(repository.Object, bookService.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(
            categoryId,
            new CategoryInput { Name = "Duplicate", Description = "Changed" },
            Array.Empty<Guid>()));

        Assert.Equal("Existing", category.Name);
        repository.Verify(value => value.UpdateAsync(It.IsAny<Category>()), Times.Never);
        bookService.Verify(value => value.UpdateAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsyncReportsNotFoundInactiveAndCategoriesWithBooks()
    {
        var repository = new Mock<ICategoryRespository>();
        var service = new CategoryService(repository.Object, Mock.Of<ICategoryBookService>());
        Guid categoryId = Guid.NewGuid();

        Assert.Equal(CategoryDeactivationResult.NotFound, await service.DeactivateAsync(categoryId));

        var inactiveCategory = new Category("Inactive", string.Empty, isActive: false);
        repository.Setup(value => value.GetByIdAsync(categoryId)).ReturnsAsync(inactiveCategory);
        Assert.Equal(CategoryDeactivationResult.AlreadyInactive, await service.DeactivateAsync(categoryId));
        repository.Verify(value => value.HasBooksAsync(categoryId), Times.Never);

        var activeCategory = new Category("In use", string.Empty);
        repository.Setup(value => value.GetByIdAsync(categoryId)).ReturnsAsync(activeCategory);
        repository.Setup(value => value.HasBooksAsync(categoryId)).ReturnsAsync(true);
        Assert.Equal(CategoryDeactivationResult.HasBooks, await service.DeactivateAsync(categoryId));
        repository.Verify(value => value.DeleteAsync(categoryId), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsyncSoftDeletesUnusedCategoryAndReportsFailedDelete()
    {
        var repository = new Mock<ICategoryRespository>();
        Guid categoryId = Guid.NewGuid();
        var category = new Category("Unused", string.Empty);
        repository.Setup(value => value.GetByIdAsync(categoryId)).ReturnsAsync(category);
        repository.Setup(value => value.HasBooksAsync(categoryId)).ReturnsAsync(false);
        repository.Setup(value => value.DeleteAsync(categoryId)).ReturnsAsync(true);
        var service = new CategoryService(repository.Object, Mock.Of<ICategoryBookService>());

        Assert.Equal(CategoryDeactivationResult.Deactivated, await service.DeactivateAsync(categoryId));
        repository.Verify(value => value.DeleteAsync(categoryId), Times.Once);

        var stillActiveCategory = new Category("Still active", string.Empty);
        repository.Setup(value => value.GetByIdAsync(categoryId)).ReturnsAsync(stillActiveCategory);
        repository.Setup(value => value.DeleteAsync(categoryId)).ReturnsAsync(false);
        Assert.Equal(CategoryDeactivationResult.Failed, await service.DeactivateAsync(categoryId));
    }

    [Fact]
    public async Task CategoryServiceForwardsRelationshipQueryMethods()
    {
        var repository = new Mock<ICategoryRespository>();
        Guid categoryId = Guid.NewGuid();
        Category category = new("History", "Stories");
        IList<Category?> activeCategories = [category];
        repository.Setup(value => value.GetByIdWithBooksAsync(categoryId)).ReturnsAsync(category);
        repository.Setup(value => value.GetAllActiveAsync()).ReturnsAsync(activeCategories);
        var service = new CategoryService(repository.Object, Mock.Of<ICategoryBookService>());

        Assert.Same(category, await service.GetByIdWithBooksAsync(categoryId));
        Assert.Same(activeCategories, await service.GetAllActiveAsync());

        repository.Verify(value => value.GetByIdWithBooksAsync(categoryId), Times.Once);
        repository.Verify(value => value.GetAllActiveAsync(), Times.Once);
    }

    private static void SetId(Entity<Guid> entity, Guid id) =>
        typeof(Entity<Guid>).GetProperty(nameof(Entity<Guid>.Id))!.SetValue(entity, id);
}
