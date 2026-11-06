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

public sealed class CategoriesCrudIntegrationTests : IntegrationTestBase
{
    private readonly IntegrationDatabaseFixture _fixture;

    public CategoriesCrudIntegrationTests(IntegrationDatabaseFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AdminCanCreateEditDeactivateAndRestoreCategoryOverHttp()
    {
        TestAccount admin = await IntegrationIdentity.CreateUserAsync(_fixture, RoleNames.Admin);
        using HttpResponseMessage loginResponse = await LoginAsync(Client, admin.Email, admin.Password);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        string originalName = $"Integration category {Guid.NewGuid():N}";
        string originalDescription = $"Created description {Guid.NewGuid():N}";
        string updatedName = $"Updated category {Guid.NewGuid():N}";
        string updatedDescription = $"Updated description {Guid.NewGuid():N}";

        IDocument createPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Category/Create");
        using HttpResponseMessage createResponse = await Client.PostAsync(
            "/Category/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = HtmlTestHelpers.GetAntiforgeryToken(createPage),
                ["Name"] = originalName,
                ["Description"] = originalDescription,
                ["IsActive"] = "true"
            }));

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        Assert.StartsWith("/Category/Edit/", HtmlTestHelpers.GetRedirectPathAndQuery(createResponse));

        Category createdCategory = await ReadCategoryByNameAsync(originalName);
        Assert.Equal(originalDescription, createdCategory.Description);
        Assert.True(createdCategory.IsActive);

        IDocument categoryList = await HtmlTestHelpers.GetDocumentAsync(Client, "/Category/Index");
        IElement createdRow = FindCategoryRow(categoryList, originalName);
        Assert.NotNull(createdRow.QuerySelector(".obs-status-dot.active"));

        string detailsPath = $"/Category/Details/{createdCategory.Id}";
        IDocument detailsPage = await HtmlTestHelpers.GetDocumentAsync(Client, detailsPath);
        Assert.Contains(originalName, detailsPage.Body?.TextContent);
        Assert.Contains(originalDescription, detailsPage.Body?.TextContent);
        Assert.NotNull(detailsPage.QuerySelector(".obs-status-dot.active"));

        string editPath = $"/Category/Edit/{createdCategory.Id}";
        IDocument editPage = await HtmlTestHelpers.GetDocumentAsync(Client, editPath);
        using HttpResponseMessage editResponse = await Client.PostAsync(
            editPath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = HtmlTestHelpers.GetAntiforgeryToken(editPage),
                ["Id"] = createdCategory.Id.ToString(),
                ["Name"] = updatedName,
                ["Description"] = updatedDescription,
                ["IsActive"] = "true"
            }));

        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);
        Category editedCategory = await ReadCategoryByIdAsync(createdCategory.Id);
        Assert.Equal(updatedName, editedCategory.Name);
        Assert.Equal(updatedDescription, editedCategory.Description);
        Assert.True(editedCategory.IsActive);

        detailsPage = await HtmlTestHelpers.GetDocumentAsync(Client, detailsPath);
        Assert.Contains(updatedName, detailsPage.Body?.TextContent);
        Assert.Contains(updatedDescription, detailsPage.Body?.TextContent);

        categoryList = await HtmlTestHelpers.GetDocumentAsync(Client, "/Category/Index");
        using HttpResponseMessage deactivateResponse = await Client.PostAsync(
            $"/Category/Deactivate/{createdCategory.Id}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = HtmlTestHelpers.GetAntiforgeryToken(categoryList)
            }));

        Assert.Equal(HttpStatusCode.Redirect, deactivateResponse.StatusCode);
        Category deactivatedCategory = await ReadCategoryByIdAsync(createdCategory.Id);
        Assert.False(deactivatedCategory.IsActive);
        Assert.NotNull(deactivatedCategory.DeletedAt);

        categoryList = await HtmlTestHelpers.GetDocumentAsync(Client, "/Category/Index");
        IElement inactiveRow = FindCategoryRow(categoryList, updatedName);
        Assert.NotNull(inactiveRow.QuerySelector(".obs-status-dot.inactive"));
        detailsPage = await HtmlTestHelpers.GetDocumentAsync(Client, detailsPath);
        Assert.NotNull(detailsPage.QuerySelector(".obs-status-dot.inactive"));

        using HttpResponseMessage restoreResponse = await Client.PostAsync(
            $"/Category/Restore/{createdCategory.Id}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = HtmlTestHelpers.GetAntiforgeryToken(categoryList)
            }));

        Assert.Equal(HttpStatusCode.Redirect, restoreResponse.StatusCode);
        Category restoredCategory = await ReadCategoryByIdAsync(createdCategory.Id);
        Assert.True(restoredCategory.IsActive);
        Assert.Null(restoredCategory.DeletedAt);

        categoryList = await HtmlTestHelpers.GetDocumentAsync(Client, "/Category/Index");
        IElement restoredRow = FindCategoryRow(categoryList, updatedName);
        Assert.NotNull(restoredRow.QuerySelector(".obs-status-dot.active"));
    }

    private async Task<HttpResponseMessage> LoginAsync(HttpClient client, string userNameOrEmail, string password)
    {
        IDocument loginPage = await HtmlTestHelpers.GetDocumentAsync(client, "/Account/Login");
        return await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = HtmlTestHelpers.GetAntiforgeryToken(loginPage),
                ["UserNameOrEmail"] = userNameOrEmail,
                ["Password"] = password,
                ["RememberMe"] = "false"
            }));
    }

    private async Task<Category> ReadCategoryByNameAsync(string name)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Categories.AsNoTracking().SingleAsync(category => category.Name == name);
    }

    private async Task<Category> ReadCategoryByIdAsync(Guid id)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Categories.AsNoTracking().SingleAsync(category => category.Id == id);
    }

    private static IElement FindCategoryRow(IDocument document, string name)
    {
        return document.QuerySelectorAll("tbody tr")
            .Single(row => row.TextContent.Contains(name, StringComparison.Ordinal));
    }
}
