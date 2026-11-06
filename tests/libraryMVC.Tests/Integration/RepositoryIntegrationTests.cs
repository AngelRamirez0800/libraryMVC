using libraryMVC.Data;
using libraryMVC.Data.Repositories;
using libraryMVC.DTOs;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using libraryMVC.Tests.Integration.Infrastructure;

namespace libraryMVC.Tests.Integration;

public sealed class RepositoryIntegrationTests : IntegrationTestBase
{
    private readonly IntegrationDatabaseFixture _fixture;

    public RepositoryIntegrationTests(IntegrationDatabaseFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GenericRepositorySupportsQueriesCrudAndSoftDeleteLifecycle()
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = new Repository<Book, Guid>(context);
        IList<Book?> seededBooks = await repository.GetAllAsync();

        Assert.Equal(10, seededBooks.Count);
        Assert.Equal(10, (await repository.GetAllActiveAsync()).Count);
        Assert.Empty(await repository.GetAllInactiveAsync());

        Book existing = Assert.IsType<Book>(seededBooks[0]);
        Assert.True(await repository.AlreadyExistsAsync(existing.Id));
        Assert.False(await repository.AlreadyExistsAsync(Guid.NewGuid()));
        Assert.Same(existing, await repository.GetByIdAsync(existing.Id));

        Assert.True(await repository.DeleteAsync(existing.Id));
        Assert.False(existing.IsActive);
        Assert.NotNull(existing.DeletedAt);
        Assert.False(await repository.DeleteAsync(existing.Id));
        Assert.Single(await repository.GetAllInactiveAsync());

        Assert.True(await repository.RestoreAsync(existing.Id));
        Assert.True(existing.IsActive);
        Assert.Null(existing.DeletedAt);
        Assert.False(await repository.RestoreAsync(existing.Id));

        Book added = await repository.AddAsync(new Book("979-1-234567-89-0", "Repository addition", "Added through the generic repository."));
        Assert.NotEqual(Guid.Empty, added.Id);
        added.UpdateDetails(added.Isbn, "Repository update", "Updated through the generic repository.");
        Assert.Same(added, await repository.UpdateAsync(added));

        context.ChangeTracker.Clear();
        Assert.Equal("Repository update", (await repository.GetByIdAsync(added.Id))?.Title);
        Assert.Equal(11, (await repository.GetAllAsync()).Count);
    }

    [Fact]
    public async Task SpecializedRepositoriesLoadRelatedDataAndAnswerCategoryQueries()
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        List<Guid> authorIds = await context.Authors.Select(author => author.Id).ToListAsync();
        List<Guid> bookIds = await context.Books.Select(book => book.Id).ToListAsync();
        List<Guid> categoryIds = await context.Categories.Select(category => category.Id).ToListAsync();

        var authorsRepository = new AuthorsRepository(context);
        Author? author = await authorsRepository.GetByIdWithBooksAsync(authorIds[0]);
        Assert.NotNull(author);
        Assert.NotEmpty(author.Books);

        var booksRepository = new BooksRepository(context);
        Book? book = await booksRepository.GetByIdWithDetailsAsync(bookIds[0]);
        Assert.NotNull(book);
        Assert.NotEmpty(book.Authors);
        Assert.NotEmpty(book.Categories);
        Assert.Single(book.Images);
        Assert.Equal(10, (await booksRepository.GetAllAsync()).Count);
        Assert.Equal(3, (await booksRepository.GetRandomFeaturedBooksAsync()).Count);
        Assert.Empty(await booksRepository.GetRandomFeaturedBooksAsync(0));
        Assert.Equal(10, (await booksRepository.GetRandomFeaturedBooksAsync(20)).Count);

        var categoriesRepository = new CategoryRepository(context);
        Category? category = await categoriesRepository.GetByIdWithBooksAsync(categoryIds[0]);
        Assert.NotNull(category);
        Assert.NotEmpty(category.Books);
        Assert.Equal(5, (await categoriesRepository.GetAllAsync()).Count);
        Assert.True(await categoriesRepository.HasBooksAsync(categoryIds[0]));
        Assert.False(await categoriesRepository.HasBooksAsync(Guid.NewGuid()));
        Assert.True(await categoriesRepository.NameExistsAsync(category.Name.ToUpperInvariant()));
        Assert.False(await categoriesRepository.NameExistsAsync(category.Name, category.Id));
        Assert.False(await categoriesRepository.NameExistsAsync("A category that does not exist"));
    }

    [Fact]
    public async Task RelationshipRepositoriesReplaceAssociationsAndPreserveCategoryInvariants()
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        List<Author> authors = await context.Authors.ToListAsync();
        List<Book> books = await context.Books
            .Include(book => book.Authors)
            .Include(book => book.Categories)
            .ToListAsync();
        List<Category> categories = await context.Categories.ToListAsync();
        Book book = books[0];

        var bookAuthorRepository = new BookAuthorRepository(context);
        await bookAuthorRepository.UpdateAsync(book.Id, [authors[1].Id]);
        Assert.Equal(authors[1].Id, Assert.Single(book.Authors).Id);
        await bookAuthorRepository.UpdateAsync(Guid.NewGuid(), [authors[0].Id]);

        var authorBookRepository = new AuthorBookRepository(context);
        await authorBookRepository.UpdateAsync(authors[0].Id, [book.Id]);
        Assert.Equal(book.Id, Assert.Single(authors[0].Books).Id);
        await authorBookRepository.UpdateAsync(Guid.NewGuid(), [book.Id]);

        var categoryBookRepository = new CategoryBookRepository(context);
        await categoryBookRepository.UpdateForBookAsync(book.Id, [categories[0].Id]);
        Assert.Equal(categories[0].Id, Assert.Single(book.Categories).Id);
        Assert.Contains(categories[0].Books, relatedBook => relatedBook.Id == book.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            categoryBookRepository.UpdateAsync(categories[0].Id, Array.Empty<Guid>()));

        await categoryBookRepository.UpdateAsync(categories[0].Id, [book.Id]);
        Assert.Equal(book.Id, Assert.Single(categories[0].Books).Id);
        await categoryBookRepository.UpdateAsync(Guid.NewGuid(), [book.Id]);
        await categoryBookRepository.UpdateForBookAsync(Guid.NewGuid(), [categories[0].Id]);
    }

    [Fact]
    public async Task CategorySoftDeleteIsBlockedByRelatedActiveBookOrAuthor()
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var activeBookCategory = new Category($"Active book category {Guid.NewGuid():N}", "In use by an active book.");
        var activeBook = new Book("978-9-123456-78-0", "Active related book", "An active book keeps the category in use.");
        activeBook.MarkCreated();
        activeBookCategory.MarkCreated();
        activeBook.ReplaceCategories([activeBookCategory]);
        activeBookCategory.ReplaceBooks([activeBook]);

        var activeAuthorCategory = new Category($"Active author category {Guid.NewGuid():N}", "Related through a book.");
        var inactiveBook = new Book(
            "978-9-123456-78-1",
            "Inactive related book",
            "The active author remains related through this book.",
            isActive: false);
        inactiveBook.MarkCreated();
        activeAuthorCategory.MarkCreated();
        var activeAuthor = new Author("Active", "Author", "Test", new DateTime(1980, 1, 1));
        activeAuthor.MarkCreated();
        inactiveBook.ReplaceCategories([activeAuthorCategory]);
        inactiveBook.ReplaceAuthors([activeAuthor]);
        activeAuthor.ReplaceBooks([inactiveBook]);
        activeAuthorCategory.ReplaceBooks([inactiveBook]);

        context.AddRange(activeBookCategory, activeBook, activeAuthorCategory, inactiveBook, activeAuthor);
        await context.SaveChangesAsync();

        var categoryRepository = new CategoryRepository(context);
        var categoryService = new CategoryService(categoryRepository, Mock.Of<ICategoryBookService>());

        Assert.Equal(CategoryDeactivationResult.HasBooks, await categoryService.DeactivateAsync(activeBookCategory.Id));
        Assert.True(activeBookCategory.IsActive);
        Assert.Null(activeBookCategory.DeletedAt);

        Assert.Equal(CategoryDeactivationResult.HasBooks, await categoryService.DeactivateAsync(activeAuthorCategory.Id));
        Assert.True(activeAuthorCategory.IsActive);
        Assert.Null(activeAuthorCategory.DeletedAt);
    }

    [Fact]
    public async Task BookImageRepositoryAddsReadsAndRemovesImageRecords()
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Guid bookId = await context.Books.Select(book => book.Id).FirstAsync();
        var repository = new BookImageRepository(context);
        BookImage image = new(bookId, "integration/repository-image.png", 2, "repository-image.png", 128);

        BookImage added = await repository.AddAsync(image);

        Assert.Same(image, added);
        Assert.NotEqual(Guid.Empty, added.Id);
        Assert.Same(added, await repository.GetByIdAsync(added.Id));

        await repository.RemoveAsync(added);

        Assert.Null(await repository.GetByIdAsync(added.Id));
    }

    [Fact]
    public async Task DatabaseUniqueCategoryIndexRejectsConcurrentDuplicateName()
    {
        string duplicateName = $"Concurrent category {Guid.NewGuid():N}";
        await using var firstScope = _fixture.Factory.Services.CreateAsyncScope();
        await using var secondScope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext firstContext = firstScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ApplicationDbContext secondContext = secondScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var firstRepository = new CategoryRepository(firstContext);
        var secondRepository = new CategoryRepository(secondContext);

        Assert.False(await firstRepository.NameExistsAsync(duplicateName));
        Assert.False(await secondRepository.NameExistsAsync(duplicateName));

        Task<bool> firstInsert = TryInsertCategoryAsync(firstRepository, duplicateName);
        Task<bool> secondInsert = TryInsertCategoryAsync(secondRepository, duplicateName);
        bool[] inserted = await Task.WhenAll(firstInsert, secondInsert);

        Assert.Single(inserted, succeeded => succeeded);

        await using var verificationScope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await verificationContext.Categories.CountAsync(category => category.Name == duplicateName));
    }

    [Fact]
    public void DbContextModelDeclaresRelationalConstraintsForImagesAndCategories()
    {
        using IServiceScope scope = _fixture.Factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var imageEntity = context.Model.FindEntityType(typeof(BookImage));
        var categoryEntity = context.Model.FindEntityType(typeof(Category));

        Assert.NotNull(imageEntity);
        Assert.NotNull(categoryEntity);
        Assert.Equal(500, imageEntity.FindProperty(nameof(BookImage.ImagePath))?.GetMaxLength());
        Assert.Equal(255, imageEntity.FindProperty(nameof(BookImage.OriginalFileName))?.GetMaxLength());
        Assert.Equal(100, categoryEntity.FindProperty(nameof(Category.Name))?.GetMaxLength());
        Assert.Contains(
            categoryEntity.GetIndexes(),
            index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(Category.Name)]));
        Assert.Contains(
            imageEntity.GetForeignKeys(),
            foreignKey => foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(BookImage.BookId)])
                && foreignKey.DeleteBehavior == DeleteBehavior.Cascade);
    }

    private static async Task<bool> TryInsertCategoryAsync(CategoryRepository repository, string name)
    {
        try
        {
            await repository.AddAsync(new Category(name, "A concurrent insert."));
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}
