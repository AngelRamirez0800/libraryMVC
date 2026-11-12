using libraryMVC.Data;
using libraryMVC.Data.Repositories;
using libraryMVC.DTOs;
using libraryMVC.DTOs.Lists;
using libraryMVC.DTOs.Reports;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using libraryMVC.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

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
    public async Task PagedQueriesApplyFiltersBeforeStablePagination()
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Author author = await context.Authors.OrderBy(entity => entity.LastName).FirstAsync();
        Category category = await context.Categories.OrderBy(entity => entity.Name).FirstAsync();
        const string titlePrefix = "Paged navigation fixture";
        string[] isbns = Enumerable.Range(0, 17)
            .Select(_ => $"978{Guid.NewGuid():N}"[..13])
            .ToArray();

        foreach (int index in Enumerable.Range(0, isbns.Length))
        {
            var book = new Book(isbns[index], $"{titlePrefix} {index:D2}", "Used to validate stable pagination.");
            book.ReplaceAuthors([author]);
            book.ReplaceCategories([category]);
            context.Books.Add(book);
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var booksRepository = new BooksRepository(context);
        var filter = new BookListFilter
        {
            Search = titlePrefix,
            AuthorId = author.Id,
            CategoryId = category.Id
        };
        var allPages = new List<BookListItem>();
        for (int pageNumber = 1; pageNumber <= 4; pageNumber++)
        {
            PageSlice<BookListItem> page = await booksRepository.GetPageAsync(
                filter,
                pageNumber,
                5,
                TestContext.Current.CancellationToken);
            Assert.Equal(pageNumber < 4, page.HasMore);
            allPages.AddRange(page.Items);
        }

        Assert.Equal(17, allPages.Count);
        Assert.Equal(17, allPages.Select(book => book.Id).Distinct().Count());
        Assert.Equal(
            Enumerable.Range(0, 17).Select(index => $"{titlePrefix} {index:D2}"),
            allPages.Select(book => book.Title));

        PageSlice<BookListItem> isbnMatch = await booksRepository.GetPageAsync(
            new BookListFilter
            {
                Search = isbns[0][3..10],
                AuthorId = author.Id,
                CategoryId = category.Id
            },
            1,
            5,
            TestContext.Current.CancellationToken);
        Assert.Equal(isbns[0], Assert.Single(isbnMatch.Items).Isbn);

        PageSlice<BookListItem> noMatch = await booksRepository.GetPageAsync(
            new BookListFilter { Search = titlePrefix, AuthorId = Guid.NewGuid() },
            1,
            5,
            TestContext.Current.CancellationToken);
        Assert.Empty(noMatch.Items);
        Assert.False(noMatch.HasMore);

        var authorsRepository = new AuthorsRepository(context);
        PageSlice<AuthorListItem> authorPage = await authorsRepository.GetPageAsync(
            new AuthorListFilter { Search = author.LastName, Nationality = author.Nationality },
            1,
            5,
            TestContext.Current.CancellationToken);
        Assert.Contains(authorPage.Items, item => item.Id == author.Id);
        Assert.Contains(author.Nationality, await authorsRepository.GetNationalitiesAsync(
            TestContext.Current.CancellationToken));
        PageSlice<AuthorListItem> firstAuthorPage = await authorsRepository.GetPageAsync(
            new AuthorListFilter(),
            1,
            1,
            TestContext.Current.CancellationToken);
        PageSlice<AuthorListItem> secondAuthorPage = await authorsRepository.GetPageAsync(
            new AuthorListFilter(),
            2,
            1,
            TestContext.Current.CancellationToken);
        Assert.True(firstAuthorPage.HasMore);
        Assert.False(secondAuthorPage.HasMore);
        Assert.NotEqual(firstAuthorPage.Items[0].Id, secondAuthorPage.Items[0].Id);

        var categoriesRepository = new CategoryRepository(context);
        PageSlice<CategoryListItem> categoryPage = await categoriesRepository.GetPageAsync(
            new CategoryListFilter { Search = category.Name },
            1,
            5,
            TestContext.Current.CancellationToken);
        CategoryListItem categoryItem = Assert.Single(categoryPage.Items, item => item.Id == category.Id);
        Assert.True(categoryItem.BookCount >= 17);
        PageSlice<CategoryListItem> firstCategoryPage = await categoriesRepository.GetPageAsync(
            new CategoryListFilter(),
            1,
            2,
            TestContext.Current.CancellationToken);
        PageSlice<CategoryListItem> secondCategoryPage = await categoriesRepository.GetPageAsync(
            new CategoryListFilter(),
            2,
            2,
            TestContext.Current.CancellationToken);
        PageSlice<CategoryListItem> lastCategoryPage = await categoriesRepository.GetPageAsync(
            new CategoryListFilter(),
            3,
            2,
            TestContext.Current.CancellationToken);
        Assert.True(firstCategoryPage.HasMore);
        Assert.True(secondCategoryPage.HasMore);
        Assert.False(lastCategoryPage.HasMore);
        Assert.Equal(5, firstCategoryPage.Items
            .Concat(secondCategoryPage.Items)
            .Concat(lastCategoryPage.Items)
            .Select(item => item.Id)
            .Distinct()
            .Count());
    }

    [Fact]
    public async Task ReportRepositoryIncludesRelatedDataAndInactiveRecords()
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = new ReportDataRepository(context);

        BookReportData firstBook = (await repository.GetBooksAsync()).First();
        IReadOnlyList<AuthorReportData> authors = await repository.GetAuthorsAsync();
        IReadOnlyList<CategoryReportData> categories = await repository.GetCategoriesAsync();

        Assert.NotEmpty(firstBook.Authors);
        Assert.NotEmpty(firstBook.Categories);
        Assert.Equal("01.png", Assert.Single(firstBook.Images).OriginalFileName);
        Assert.EndsWith("/01.png", firstBook.Images[0].ImagePath);
        Assert.Contains(authors, author => author.BookTitles.Count > 0);
        Assert.Contains(categories, category => category.BookTitles.Count > 0);
        Assert.Equal(10, (await repository.GetBooksAsync()).Count);
        IReadOnlyList<BookReportData> filteredBooks = await repository.GetBooksAsync(
            new ReportFilters(Search: firstBook.Title));
        Assert.Equal(firstBook.Title, Assert.Single(filteredBooks).Title);
        IReadOnlyList<AuthorReportData> filteredAuthors = await repository.GetAuthorsAsync(
            new ReportFilters(Search: authors[0].LastName, Nationality: authors[0].Nationality));
        Assert.Contains(filteredAuthors, author => author.LastName == authors[0].LastName);
        IReadOnlyList<CategoryReportData> filteredCategories = await repository.GetCategoriesAsync(
            new ReportFilters(Search: categories[0].Name));
        Assert.Equal(categories[0].Name, Assert.Single(filteredCategories).Name);

        Book book = await context.Books.OrderBy(entity => entity.Title).FirstAsync();
        book.Deactivate();
        await context.SaveChangesAsync();

        IReadOnlyList<BookReportData> booksAfterDeactivation = await repository.GetBooksAsync();
        Assert.Equal(10, booksAfterDeactivation.Count);
        Assert.Contains(booksAfterDeactivation, entity => entity.Title == book.Title && !entity.IsActive);
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
