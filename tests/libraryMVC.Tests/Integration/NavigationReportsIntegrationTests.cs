using System.Net;
using AngleSharp;
using AngleSharp.Dom;
using ClosedXML.Excel;
using libraryMVC.Data;
using libraryMVC.Models;
using libraryMVC.Security;
using libraryMVC.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace libraryMVC.Tests.Integration;

public sealed class NavigationReportsIntegrationTests : IntegrationTestBase
{
    private readonly IntegrationDatabaseFixture _fixture;

    public NavigationReportsIntegrationTests(IntegrationDatabaseFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FilteredBookPagesAppendAndFilteredReportIncludesAllMatches()
    {
        const string titlePrefix = "Infinite scroll fixture";
        Guid authorId;
        Guid categoryId;
        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Author author = await context.Authors.OrderBy(item => item.LastName).FirstAsync();
            Category category = await context.Categories.OrderBy(item => item.Name).FirstAsync();
            authorId = author.Id;
            categoryId = category.Id;

            for (int index = 0; index < 17; index++)
            {
                var book = new Book(
                    $"979{Guid.NewGuid():N}"[..13],
                    $"{titlePrefix} {index:D2}",
                    "Created for filtered paging and report integration coverage.");
                book.ReplaceAuthors([author]);
                book.ReplaceCategories([category]);
                context.Books.Add(book);
            }

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        TestAccount librarian = await IntegrationIdentity.CreateUserAsync(_fixture, RoleNames.Librarian);
        using HttpClient client = _fixture.CreateClient();
        using HttpResponseMessage loginResponse = await LoginAsync(client, librarian.UserName, librarian.Password);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        string filterQuery = $"Search={Uri.EscapeDataString(titlePrefix)}&AuthorId={authorId}&CategoryId={categoryId}";
        IDocument listPage = await HtmlTestHelpers.GetDocumentAsync(client, $"/Books?{filterQuery}");
        Assert.NotNull(listPage.QuerySelector("input[name='Search']"));
        Assert.NotNull(listPage.QuerySelector("select[name='AuthorId']"));
        Assert.NotNull(listPage.QuerySelector("select[name='CategoryId']"));

        IElement listRows = Assert.IsAssignableFrom<IElement>(listPage.QuerySelector("#book-list-rows"));
        Assert.Equal(15, listRows.QuerySelectorAll("tr:not([hidden])").Length);
        Assert.Contains(
            $"authorId={authorId}",
            listPage.QuerySelector("a[href^='/Reports/books/excel']")?.GetAttribute("href"),
            StringComparison.OrdinalIgnoreCase);

        using HttpResponseMessage nextPageResponse = await client.GetAsync(
            $"/Books/LoadMore?page=2&{filterQuery}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, nextPageResponse.StatusCode);
        string nextPageHtml = await nextPageResponse.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        IDocument nextPage = await BrowsingContext.New(Configuration.Default)
            .OpenAsync(request => request.Content($"<table><tbody>{nextPageHtml}</tbody></table>"));
        Assert.Equal(2, nextPage.QuerySelectorAll("tr:not([hidden])").Length);
        Assert.Equal("false", nextPage.QuerySelector("[data-list-page-metadata]")?.GetAttribute("data-has-more"));

        using HttpResponseMessage reportResponse = await client.GetAsync(
            $"/Reports/books/excel?{filterQuery}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
        byte[] reportContent = await reportResponse.Content.ReadAsByteArrayAsync(
            TestContext.Current.CancellationToken);
        using var workbook = new XLWorkbook(new MemoryStream(reportContent));
        IXLWorksheet worksheet = workbook.Worksheet("Books");
        Assert.Equal(17, worksheet.Table(0).DataRange.RowCount());
        Assert.Equal($"{titlePrefix} 00", worksheet.Cell(5, 2).GetString());
        Assert.Equal("Image Count", worksheet.Cell(4, 8).GetString());
        Assert.Equal(string.Empty, worksheet.Cell(4, 9).GetString());
    }

    [Fact]
    public async Task AuthorAndCategoryListsExposeTheirRequestedFilters()
    {
        TestAccount librarian = await IntegrationIdentity.CreateUserAsync(_fixture, RoleNames.Librarian);
        using HttpClient client = _fixture.CreateClient();
        using HttpResponseMessage loginResponse = await LoginAsync(client, librarian.UserName, librarian.Password);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        IDocument authors = await HtmlTestHelpers.GetDocumentAsync(client, "/Authors");
        Assert.NotNull(authors.QuerySelector("input[name='Search']"));
        Assert.NotNull(authors.QuerySelector("select[name='Nationality']"));

        IDocument categories = await HtmlTestHelpers.GetDocumentAsync(client, "/Category");
        Assert.NotNull(categories.QuerySelector("input[name='Search']"));
        Assert.NotNull(categories.QuerySelector("#category-list-rows[data-load-more-url]"));
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
        return await client.PostAsync("/Account/Login", loginForm);
    }
}
