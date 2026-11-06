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

public sealed class AuthorsCrudIntegrationTests : IntegrationTestBase
{
    private readonly IntegrationDatabaseFixture _fixture;

    public AuthorsCrudIntegrationTests(IntegrationDatabaseFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AdminCanCreateListEditDeactivateAndRestoreAuthorOverHttp()
    {
        TestAccount admin = await IntegrationIdentity.CreateUserAsync(_fixture, RoleNames.Admin);
        using (HttpResponseMessage login = await LoginAsAdminAsync(admin))
        {
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        }

        string unique = Guid.NewGuid().ToString("N");
        string firstName = $"Http{unique[..12]}";
        const string lastName = "Author";
        const string nationality = "Testland";
        const string updatedLastName = "Updated";
        const string updatedNationality = "Updated Testland";
        const string birthDate = "1984-05-06";

        IDocument createPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Authors/Create");
        using HttpResponseMessage create = await PostFormAsync(
            "/Authors/Create",
            HtmlTestHelpers.GetAntiforgeryToken(createPage),
            ("FirstName", firstName),
            ("LastName", lastName),
            ("Nationality", nationality),
            ("BirthDate", birthDate));
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        Assert.Equal("/Authors", HtmlTestHelpers.GetRedirectPathAndQuery(create));

        IDocument authorList = await HtmlTestHelpers.GetDocumentAsync(Client, "/Authors/Index");
        Assert.Contains($"{firstName} {lastName}", authorList.Body?.TextContent);
        Assert.Contains("Active", authorList.Body?.TextContent);

        Author createdAuthor = await FindAuthorAsync(firstName);
        Guid authorId = createdAuthor.Id;
        Assert.NotEqual(Guid.Empty, authorId);
        Assert.Equal(nationality, createdAuthor.Nationality);
        Assert.Equal(new DateTime(1984, 5, 6), createdAuthor.BirthDate.Date);
        Assert.True(createdAuthor.IsActive);
        Assert.Null(createdAuthor.DeletedAt);

        IDocument details = await HtmlTestHelpers.GetDocumentAsync(Client, $"/Authors/Details/{authorId:D}");
        Assert.Contains($"{firstName} {lastName}", details.Body?.TextContent);
        Assert.Contains(nationality, details.Body?.TextContent);
        Assert.Contains("Active", details.Body?.TextContent);

        IDocument editPage = await HtmlTestHelpers.GetDocumentAsync(Client, $"/Authors/Edit/{authorId:D}");
        using HttpResponseMessage edit = await PostFormAsync(
            $"/Authors/Edit/{authorId:D}",
            HtmlTestHelpers.GetAntiforgeryToken(editPage),
            ("Id", authorId.ToString()),
            ("FirstName", firstName),
            ("LastName", updatedLastName),
            ("Nationality", updatedNationality),
            ("BirthDate", birthDate),
            ("IsActive", "true"));
        Assert.Equal(HttpStatusCode.Redirect, edit.StatusCode);
        Assert.Equal($"/Authors/Edit/{authorId:D}", HtmlTestHelpers.GetRedirectPathAndQuery(edit));

        IDocument editedPage = await HtmlTestHelpers.GetDocumentAsync(Client, $"/Authors/Edit/{authorId:D}");
        Assert.Equal(updatedLastName, editedPage.QuerySelector("input[name='LastName']")?.GetAttribute("value"));
        Assert.Equal(updatedNationality, editedPage.QuerySelector("input[name='Nationality']")?.GetAttribute("value"));
        Author editedAuthor = await GetAuthorAsync(authorId);
        Assert.Equal(updatedLastName, editedAuthor.LastName);
        Assert.Equal(updatedNationality, editedAuthor.Nationality);
        Assert.Equal(new DateTime(1984, 5, 6), editedAuthor.BirthDate.Date);
        Assert.True(editedAuthor.IsActive);
        Assert.Null(editedAuthor.DeletedAt);

        IDocument activeList = await HtmlTestHelpers.GetDocumentAsync(Client, "/Authors/Index");
        using HttpResponseMessage deactivate = await PostFormAsync(
            $"/Authors/Deactivate/{authorId:D}",
            HtmlTestHelpers.GetAntiforgeryToken(activeList));
        Assert.Equal(HttpStatusCode.Redirect, deactivate.StatusCode);
        Assert.Equal("/Authors", HtmlTestHelpers.GetRedirectPathAndQuery(deactivate));

        IDocument inactiveList = await HtmlTestHelpers.GetDocumentAsync(Client, "/Authors/Index");
        Assert.Contains($"{firstName} {updatedLastName}", inactiveList.Body?.TextContent);
        Assert.Contains("Inactive", inactiveList.Body?.TextContent);
        IDocument inactiveDetails = await HtmlTestHelpers.GetDocumentAsync(Client, $"/Authors/Details/{authorId:D}");
        Assert.Contains("Inactive", inactiveDetails.Body?.TextContent);
        Assert.Contains("Deleted At", inactiveDetails.Body?.TextContent);

        Author deactivatedAuthor = await GetAuthorAsync(authorId);
        Assert.False(deactivatedAuthor.IsActive);
        Assert.NotNull(deactivatedAuthor.DeletedAt);
        Assert.Equal(updatedLastName, deactivatedAuthor.LastName);

        using HttpResponseMessage restore = await PostFormAsync(
            $"/Authors/Restore/{authorId:D}",
            HtmlTestHelpers.GetAntiforgeryToken(inactiveList));
        Assert.Equal(HttpStatusCode.Redirect, restore.StatusCode);
        Assert.Equal("/Authors", HtmlTestHelpers.GetRedirectPathAndQuery(restore));

        IDocument restoredList = await HtmlTestHelpers.GetDocumentAsync(Client, "/Authors/Index");
        Assert.Contains($"{firstName} {updatedLastName}", restoredList.Body?.TextContent);
        Assert.Contains("Active", restoredList.Body?.TextContent);
        IDocument restoredDetails = await HtmlTestHelpers.GetDocumentAsync(Client, $"/Authors/Details/{authorId:D}");
        Assert.Contains("Active", restoredDetails.Body?.TextContent);
        Assert.DoesNotContain("Deleted At", restoredDetails.Body?.TextContent);

        Author restoredAuthor = await GetAuthorAsync(authorId);
        Assert.True(restoredAuthor.IsActive);
        Assert.Null(restoredAuthor.DeletedAt);
        Assert.Equal(updatedLastName, restoredAuthor.LastName);
        Assert.Equal(updatedNationality, restoredAuthor.Nationality);
    }

    private async Task<HttpResponseMessage> LoginAsAdminAsync(TestAccount admin)
    {
        IDocument loginPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Account/Login");
        return await PostFormAsync(
            "/Account/Login",
            HtmlTestHelpers.GetAntiforgeryToken(loginPage),
            ("UserNameOrEmail", admin.UserName),
            ("Password", admin.Password),
            ("RememberMe", "false"),
            ("ReturnUrl", "/Authors/Index"));
    }

    private async Task<HttpResponseMessage> PostFormAsync(
        string path,
        string antiforgeryToken,
        params (string Name, string Value)[] fields)
    {
        Dictionary<string, string> values = new()
        {
            ["__RequestVerificationToken"] = antiforgeryToken
        };
        foreach ((string name, string value) in fields)
        {
            values[name] = value;
        }

        using FormUrlEncodedContent content = new(values);
        return await Client.PostAsync(path, content);
    }

    private async Task<Author> FindAuthorAsync(string firstName)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Authors.AsNoTracking().SingleAsync(author => author.FirstName == firstName);
    }

    private async Task<Author> GetAuthorAsync(Guid id)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Authors.AsNoTracking().SingleAsync(author => author.Id == id);
    }
}
