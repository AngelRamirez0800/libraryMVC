using System.Reflection;
using libraryMVC.Controllers;
using libraryMVC.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace libraryMVC.Tests.Unit.Controllers;

public sealed class ReportsControllerTests
{
    [Fact]
    public void AllReportDownloadsRequireTheBookManagerRoles()
    {
        AuthorizeAttribute controllerAuthorization = Assert.IsType<AuthorizeAttribute>(
            typeof(ReportsController).GetCustomAttribute<AuthorizeAttribute>());

        Assert.Equal(RoleNames.BookManagers, controllerAuthorization.Roles);
        Assert.Collection(
            GetDownloadActions(),
            action => Assert.Null(action.GetCustomAttribute<AuthorizeAttribute>()),
            action => Assert.Null(action.GetCustomAttribute<AuthorizeAttribute>()),
            action => Assert.Null(action.GetCustomAttribute<AuthorizeAttribute>()),
            action => Assert.Null(action.GetCustomAttribute<AuthorizeAttribute>()),
            action => Assert.Null(action.GetCustomAttribute<AuthorizeAttribute>()),
            action => Assert.Null(action.GetCustomAttribute<AuthorizeAttribute>()));
    }

    [Fact]
    public void DownloadActionsExposeDistinctGetRoutes()
    {
        string? booksRoute = GetRoute(nameof(ReportsController.BooksPdf));
        string? authorsRoute = GetRoute(nameof(ReportsController.AuthorsPdf));
        string? categoriesRoute = GetRoute(nameof(ReportsController.CategoriesPdf));

        Assert.Equal("books/pdf", booksRoute);
        Assert.Equal("authors/pdf", authorsRoute);
        Assert.Equal("categories/pdf", categoriesRoute);
    }

    private static string? GetRoute(string actionName) =>
        typeof(ReportsController)
            .GetMethod(actionName)!
            .GetCustomAttribute<HttpGetAttribute>()?
            .Template;

    private static MethodInfo[] GetDownloadActions() =>
    [
        typeof(ReportsController).GetMethod(nameof(ReportsController.BooksPdf))!,
        typeof(ReportsController).GetMethod(nameof(ReportsController.BooksExcel))!,
        typeof(ReportsController).GetMethod(nameof(ReportsController.AuthorsPdf))!,
        typeof(ReportsController).GetMethod(nameof(ReportsController.AuthorsExcel))!,
        typeof(ReportsController).GetMethod(nameof(ReportsController.CategoriesPdf))!,
        typeof(ReportsController).GetMethod(nameof(ReportsController.CategoriesExcel))!
    ];
}
