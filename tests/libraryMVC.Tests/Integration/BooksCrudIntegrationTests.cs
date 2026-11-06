using System.Net;
using AngleSharp.Dom;
using libraryMVC.Data;
using libraryMVC.Models;
using libraryMVC.Security;
using libraryMVC.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace libraryMVC.Tests.Integration;

public sealed class BooksCrudIntegrationTests : IntegrationTestBase
{
    private readonly IntegrationDatabaseFixture _fixture;

    public BooksCrudIntegrationTests(IntegrationDatabaseFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task BookCanBeCreatedListedViewedEditedDeactivatedAndRestoredOverHttp()
    {
        string uniqueId = Guid.NewGuid().ToString("N");
        string categoryName = $"CRUD category {uniqueId}";
        string authorFirstName = $"CRUD{uniqueId[..8]}";
        string originalTitle = $"CRUD book {uniqueId}";
        string editedTitle = $"Edited CRUD book {uniqueId}";
        string isbn = $"978-{uniqueId[..10]}";
        string editedIsbn = $"979-{uniqueId[..10]}";
        string originalSummary = $"Original summary {uniqueId}";
        string editedSummary = $"Edited summary {uniqueId}";

        Guid categoryId;
        Guid authorId;
        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var category = new Category(categoryName, "Seeded for the Books CRUD integration flow.");
            var author = new Author(authorFirstName, "Integration", "Test", new DateTime(1980, 1, 1));
            context.Categories.Add(category);
            context.Authors.Add(author);
            await context.SaveChangesAsync();
            categoryId = category.Id;
            authorId = author.Id;
        }

        TestAccount account = await IntegrationIdentity.CreateUserAsync(_fixture, RoleNames.Librarian);
        using HttpResponseMessage loginResponse = await LoginAsync(Client, account.UserName, account.Password);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        IDocument createPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Books/Create");
        using MultipartFormDataContent createForm = CreateMultipartForm(
            HtmlTestHelpers.GetAntiforgeryToken(createPage),
            ("Isbn", isbn),
            ("Title", originalTitle),
            ("Summary", originalSummary),
            ("IsActive", "true"),
            ("selectedAuthorIds", authorId.ToString()),
            ("selectedCategoryIds", categoryId.ToString()));
        using HttpResponseMessage createResponse = await Client.PostAsync(
            "/Books/Create", createForm);
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        Assert.Equal("/Books", HtmlTestHelpers.GetRedirectPathAndQuery(createResponse));

        Guid bookId;
        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Book createdBook = await context.Books
                .Include(book => book.Authors)
                .Include(book => book.Categories)
                .SingleAsync(book => book.Title == originalTitle);
            bookId = createdBook.Id;
            Assert.Equal(isbn, createdBook.Isbn);
            Assert.Equal(originalSummary, createdBook.Summary);
            Assert.True(createdBook.IsActive);
            Assert.Contains(createdBook.Authors, author => author.Id == authorId);
            Assert.Contains(createdBook.Categories, category => category.Id == categoryId);
        }

        IDocument listPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Books/Index");
        IElement listedBook = Assert.Single(listPage.QuerySelectorAll("tbody tr")
            .Where(row => row.TextContent.Contains(originalTitle, StringComparison.Ordinal)));
        Assert.Contains(isbn, listedBook.TextContent);
        Assert.Contains(categoryName, listedBook.TextContent);
        Assert.Contains("Active", listedBook.TextContent);

        IDocument detailsPage = await HtmlTestHelpers.GetDocumentAsync(Client, $"/Books/Details/{bookId}");
        Assert.Contains(originalTitle, detailsPage.Body?.TextContent);
        Assert.Contains(isbn, detailsPage.Body?.TextContent);
        Assert.Contains(originalSummary, detailsPage.Body?.TextContent);
        Assert.Contains(categoryName, detailsPage.Body?.TextContent);
        Assert.Contains($"{authorFirstName} Integration", detailsPage.Body?.TextContent);

        IDocument editPage = await HtmlTestHelpers.GetDocumentAsync(Client, $"/Books/Edit/{bookId}");
        using MultipartFormDataContent editForm = CreateMultipartForm(
            HtmlTestHelpers.GetAntiforgeryToken(editPage),
            ("Id", bookId.ToString()),
            ("Isbn", editedIsbn),
            ("Title", editedTitle),
            ("Summary", editedSummary),
            ("IsActive", "true"),
            ("selectedAuthorIds", authorId.ToString()),
            ("selectedCategoryIds", categoryId.ToString()));
        using HttpResponseMessage editResponse = await Client.PostAsync(
            $"/Books/Edit/{bookId}", editForm);
        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);

        IDocument editedDetailsPage = await HtmlTestHelpers.GetDocumentAsync(Client, $"/Books/Details/{bookId}");
        Assert.Contains(editedTitle, editedDetailsPage.Body?.TextContent);
        Assert.Contains(editedIsbn, editedDetailsPage.Body?.TextContent);
        Assert.Contains(editedSummary, editedDetailsPage.Body?.TextContent);
        Assert.Contains(categoryName, editedDetailsPage.Body?.TextContent);

        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Book updatedBook = await context.Books
                .Include(book => book.Authors)
                .Include(book => book.Categories)
                .SingleAsync(book => book.Id == bookId);
            Assert.Equal(editedIsbn, updatedBook.Isbn);
            Assert.Equal(editedTitle, updatedBook.Title);
            Assert.Equal(editedSummary, updatedBook.Summary);
            Assert.Contains(updatedBook.Authors, author => author.Id == authorId);
            Assert.Contains(updatedBook.Categories, category => category.Id == categoryId);
        }

        IDocument beforeDeactivatePage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Books/Index");
        using var deactivateForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = HtmlTestHelpers.GetAntiforgeryToken(beforeDeactivatePage)
        });
        using HttpResponseMessage deactivateResponse = await Client.PostAsync(
            $"/Books/Deactivate?id={bookId}", deactivateForm);
        Assert.Equal(HttpStatusCode.Redirect, deactivateResponse.StatusCode);

        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Book deactivatedBook = await context.Books.SingleAsync(book => book.Id == bookId);
            Assert.False(deactivatedBook.IsActive);
            Assert.NotNull(deactivatedBook.DeletedAt);
        }

        IDocument deactivatedListPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Books/Index");
        IElement inactiveRow = Assert.Single(deactivatedListPage.QuerySelectorAll("tbody tr")
            .Where(row => row.TextContent.Contains(editedTitle, StringComparison.Ordinal)));
        Assert.Contains("Inactive", inactiveRow.TextContent);

        using var restoreForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = HtmlTestHelpers.GetAntiforgeryToken(deactivatedListPage)
        });
        using HttpResponseMessage restoreResponse = await Client.PostAsync(
            $"/Books/Restore?id={bookId}", restoreForm);
        Assert.Equal(HttpStatusCode.Redirect, restoreResponse.StatusCode);

        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Book restoredBook = await context.Books.SingleAsync(book => book.Id == bookId);
            Assert.True(restoredBook.IsActive);
            Assert.Null(restoredBook.DeletedAt);
        }

        IDocument restoredListPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Books/Index");
        IElement restoredRow = Assert.Single(restoredListPage.QuerySelectorAll("tbody tr")
            .Where(row => row.TextContent.Contains(editedTitle, StringComparison.Ordinal)));
        Assert.Contains("Active", restoredRow.TextContent);
    }

    private static MultipartFormDataContent CreateMultipartForm(
        string antiforgeryToken,
        params (string Name, string Value)[] fields)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(antiforgeryToken), "__RequestVerificationToken");
        foreach ((string name, string value) in fields)
        {
            content.Add(new StringContent(value), name);
        }

        return content;
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string userName, string password)
    {
        IDocument loginPage = await HtmlTestHelpers.GetDocumentAsync(client, "/Account/Login");
        using var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = HtmlTestHelpers.GetAntiforgeryToken(loginPage),
            ["UserNameOrEmail"] = userName,
            ["Password"] = password,
            ["RememberMe"] = "false"
        });
        return await client.PostAsync(
            "/Account/Login", loginForm);
    }
}
