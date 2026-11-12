using System.Net;
using AngleSharp.Dom;
using libraryMVC.Data;
using libraryMVC.Models;
using libraryMVC.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace libraryMVC.Tests.Integration;

public sealed class PublicAndAuthorizationTests : IntegrationTestBase
{
    private readonly IntegrationDatabaseFixture _fixture;

    public PublicAndAuthorizationTests(IntegrationDatabaseFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublicPagesRenderHtmlAndRegistrationFormControls()
    {
        IDocument home = await HtmlTestHelpers.GetDocumentAsync(Client, "/");
        Assert.Contains("UEES Virtual Library", home.QuerySelector("main")?.TextContent);
        Assert.NotNull(home.QuerySelector("#carouselFeaturedBooks"));
        Assert.NotNull(home.QuerySelector("a[href='/Account/Login']"));
        Assert.NotNull(home.QuerySelector("a[href='/Account/Register']"));

        IDocument login = await HtmlTestHelpers.GetDocumentAsync(Client, "/Account/Login");
        Assert.NotNull(login.QuerySelector("form input[name='UserNameOrEmail']"));
        Assert.NotNull(login.QuerySelector("form input[name='Password']"));
        Assert.NotNull(login.QuerySelector("input[name='__RequestVerificationToken']"));

        IDocument registration = await HtmlTestHelpers.GetDocumentAsync(Client, "/Account/Register");
        Assert.NotNull(registration.QuerySelector("form input[name='UserName']"));
        Assert.NotNull(registration.QuerySelector("form input[name='Email']"));
        Assert.NotNull(registration.QuerySelector("form input[name='ConfirmPassword']"));
    }

    [Theory]
    [InlineData("/Home/Autores")]
    [InlineData("/Home/Categorias")]
    [InlineData("/Home/Prestamos")]
    [InlineData("/Home/AcercaDe")]
    [InlineData("/Home/Usuarios")]
    [InlineData("/Books/Index")]
    [InlineData("/Books/LoadMore?page=2")]
    [InlineData("/Authors/Index")]
    [InlineData("/Authors/LoadMore?page=2")]
    [InlineData("/Category/Index")]
    [InlineData("/Category/LoadMore?page=2")]
    [InlineData("/Reports/books/pdf")]
    [InlineData("/Reports/books/excel")]
    [InlineData("/Reports/authors/pdf")]
    [InlineData("/Reports/categories/excel")]
    public async Task AnonymousRequestsToProtectedRoutesRedirectToLogin(string path)
    {
        using HttpResponseMessage response = await Client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        string location = Uri.UnescapeDataString(HtmlTestHelpers.GetRedirectPathAndQuery(response));
        Assert.StartsWith("/Account/Login", location);
        Assert.Contains(path, location);
    }

    [Fact]
    public async Task AuthenticatedUsersCanReadBooksButCannotAccessManagerOrAdminRoutes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TestAccount account = await IntegrationIdentity.CreateUserAsync(_fixture);
        string title = $"Database-backed book {Guid.NewGuid():N}";
        await using (var scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Books.Add(new Book($"978-{Random.Shared.Next(100000000, 999999999)}", title, "Shown in the MVC inventory."));
            await context.SaveChangesAsync();
        }

        using HttpClient userClient = _fixture.CreateClient();
        using HttpResponseMessage loginResponse = await LoginAsync(userClient, account.UserName, account.Password);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        foreach (string homePath in new[]
        {
            "/Home/Categorias",
            "/Home/Prestamos",
            "/Home/AcercaDe"
        })
        {
            using HttpResponseMessage homeResponse = await userClient.GetAsync(homePath);
            Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);
            IDocument homePage = await HtmlTestHelpers.ParseAsync(homeResponse);
            Assert.NotNull(homePage.QuerySelector("main"));
        }

        using HttpResponseMessage inventoryResponse = await userClient.GetAsync("/Books/Index");
        Assert.Equal(HttpStatusCode.OK, inventoryResponse.StatusCode);
        IDocument inventory = await HtmlTestHelpers.ParseAsync(inventoryResponse);
        Assert.Contains(title, inventory.QuerySelector("tbody")?.TextContent);
        Assert.Null(inventory.QuerySelector("a[href='/Reports/books/pdf']"));
        Assert.Null(inventory.QuerySelector("a[href='/Reports/books/excel']"));
        Assert.Null(inventory.QuerySelector("a[href='/Books/Create']"));

        using HttpResponseMessage bookPageResponse = await userClient.GetAsync(
            "/Books/LoadMore?page=2",
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, bookPageResponse.StatusCode);

        using HttpResponseMessage authorsResponse = await userClient.GetAsync(
            "/Authors/Index",
            cancellationToken);
        IDocument authorsPage = await HtmlTestHelpers.ParseAsync(authorsResponse);
        Assert.Null(authorsPage.QuerySelector("a[href='/Reports/authors/pdf']"));
        Assert.Null(authorsPage.QuerySelector("a[href='/Reports/authors/excel']"));

        using HttpResponseMessage categoriesResponse = await userClient.GetAsync(
            "/Category/Index",
            cancellationToken);
        IDocument categoriesPage = await HtmlTestHelpers.ParseAsync(categoriesResponse);
        Assert.Null(categoriesPage.QuerySelector("a[href='/Reports/categories/pdf']"));
        Assert.Null(categoriesPage.QuerySelector("a[href='/Reports/categories/excel']"));

        foreach (string reportPath in new[]
        {
            "/Reports/books/pdf",
            "/Reports/books/excel",
            "/Reports/authors/pdf",
            "/Reports/authors/excel",
            "/Reports/categories/pdf",
            "/Reports/categories/excel"
        })
        {
            using HttpResponseMessage reportResponse = await userClient.GetAsync(
                reportPath,
                cancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, reportResponse.StatusCode);
            Assert.StartsWith("/Account/AccessDenied", HtmlTestHelpers.GetRedirectPathAndQuery(reportResponse));
        }

        using HttpResponseMessage createResponse = await userClient.GetAsync("/Books/Create");
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", HtmlTestHelpers.GetRedirectPathAndQuery(createResponse));

        using HttpResponseMessage adminResponse = await userClient.GetAsync("/Home/Usuarios");
        Assert.Equal(HttpStatusCode.Redirect, adminResponse.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", HtmlTestHelpers.GetRedirectPathAndQuery(adminResponse));

        TestAccount admin = await IntegrationIdentity.CreateUserAsync(_fixture, "Admin");
        using HttpClient adminClient = _fixture.CreateClient();
        using HttpResponseMessage adminLogin = await LoginAsync(adminClient, admin.Email, admin.Password);
        Assert.Equal(HttpStatusCode.Redirect, adminLogin.StatusCode);

        foreach ((string reportPath, string contentType) in new[]
        {
            ("/Reports/books/pdf", "application/pdf"),
            ("/Reports/books/excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            ("/Reports/authors/pdf", "application/pdf"),
            ("/Reports/authors/excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            ("/Reports/categories/pdf", "application/pdf"),
            ("/Reports/categories/excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        })
        {
            using HttpResponseMessage reportResponse = await adminClient.GetAsync(
                reportPath,
                cancellationToken);
            Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
            Assert.Equal(contentType, reportResponse.Content.Headers.ContentType?.MediaType);
        }

        using HttpResponseMessage adminAuthorsResponse = await adminClient.GetAsync(
            "/Authors/Index",
            cancellationToken);
        IDocument adminAuthorsPage = await HtmlTestHelpers.ParseAsync(adminAuthorsResponse);
        Assert.NotNull(adminAuthorsPage.QuerySelector("a[href='/Reports/authors/pdf']"));
        Assert.NotNull(adminAuthorsPage.QuerySelector("a[href='/Reports/authors/excel']"));

        using HttpResponseMessage adminCategoriesResponse = await adminClient.GetAsync(
            "/Category/Index",
            cancellationToken);
        IDocument adminCategoriesPage = await HtmlTestHelpers.ParseAsync(adminCategoriesResponse);
        Assert.NotNull(adminCategoriesPage.QuerySelector("a[href='/Reports/categories/pdf']"));
        Assert.NotNull(adminCategoriesPage.QuerySelector("a[href='/Reports/categories/excel']"));

        using HttpResponseMessage usersResponse = await adminClient.GetAsync("/Home/Usuarios");
        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);
        IDocument usersPage = await HtmlTestHelpers.ParseAsync(usersResponse);
        Assert.NotNull(usersPage.QuerySelector("main"));
        Assert.NotNull(usersPage.QuerySelector("nav a[href='/Home/Usuarios']"));
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string userNameOrEmail, string password)
    {
        IDocument loginPage = await HtmlTestHelpers.GetDocumentAsync(client, "/Account/Login");
        string token = HtmlTestHelpers.GetAntiforgeryToken(loginPage);
        return await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["UserNameOrEmail"] = userNameOrEmail,
            ["Password"] = password,
            ["RememberMe"] = "false"
        }));
    }
}
