using libraryMVC.Data;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using libraryMVC.Tests.Integration.Infrastructure;

namespace libraryMVC.Tests.Integration;

public sealed class DatabaseIntegrationTests : IntegrationTestBase
{
    private readonly IntegrationDatabaseFixture _fixture;

    public DatabaseIntegrationTests(IntegrationDatabaseFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StartupAppliesAllRealMigrationsAndDatabasePersistsEntities()
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        string[] expectedMigrations =
        [
            "20260723182658_InitialCreate",
            "20260811002836_AddBookImage",
            "20260911004024_AddCategoryBook",
            "20260914182915_AddDataAnnotationConstraints",
            "20261002003518_AddIdentity",
            "20261002155954_ProductsAndCategories"
        ];

        Assert.True(await context.Database.CanConnectAsync());
        Assert.Equal(expectedMigrations, await context.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());

        string title = $"Integration book {Guid.NewGuid():N}";
        var book = new Book($"978-{Random.Shared.Next(100000000, 999999999)}", title, "Persisted by an integration test.");
        context.Books.Add(book);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        Book? persistedBook = await context.Books.SingleOrDefaultAsync(item => item.Title == title);
        Assert.NotNull(persistedBook);
        Assert.NotEqual(Guid.Empty, persistedBook.Id);
    }

    [Fact]
    public async Task BogusSeedContainsExpectedGraphAndRealTemporaryImages()
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        List<Book> books = await context.Books
            .Include(book => book.Authors)
            .Include(book => book.Categories)
            .Include(book => book.Images)
            .ToListAsync();
        List<Author> authors = await context.Authors.Include(author => author.Books).ToListAsync();
        List<Category> categories = await context.Categories.Include(category => category.Books).ToListAsync();

        Assert.Equal(2, authors.Count);
        Assert.Equal(10, books.Count);
        Assert.Equal(5, categories.Count);
        Assert.All(books, book =>
        {
            Assert.NotEmpty(book.Authors);
            Assert.NotEmpty(book.Categories);
            Assert.Single(book.Images);
            Assert.True(File.Exists(Path.Combine(_fixture.WebRootPath, book.Images.Single().ImagePath)));
        });
        Assert.All(authors, author => Assert.NotEmpty(author.Books));
        Assert.All(categories, category => Assert.NotEmpty(category.Books));
        Assert.All(authors, author =>
            Assert.All(categories, category =>
                Assert.Contains(author.Books, book => book.Categories.Any(item => item.Id == category.Id))));
    }
}
