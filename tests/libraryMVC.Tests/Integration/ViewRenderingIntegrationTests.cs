using AngleSharp;
using AngleSharp.Dom;
using libraryMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using libraryMVC.Tests.Integration.Infrastructure;

namespace libraryMVC.Tests.Integration;

public sealed class ViewRenderingIntegrationTests : IntegrationTestBase
{
    private readonly IntegrationDatabaseFixture _fixture;

    public ViewRenderingIntegrationTests(IntegrationDatabaseFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ErrorAccessDeniedAndUnroutedAuthorDeleteViewsRenderHtml()
    {
        IDocument errorPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Home/Error");
        Assert.Contains("No se pudo completar la solicitud", errorPage.Body?.TextContent);
        Assert.NotNull(errorPage.QuerySelector("main"));

        IDocument accessDeniedPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Account/AccessDenied");
        Assert.Contains("No tienes permiso para acceder a esta página", accessDeniedPage.Body?.TextContent);
        Assert.NotNull(accessDeniedPage.QuerySelector("a[href]"));

        using IServiceScope scope = _fixture.Factory.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };
        var routeData = new RouteData();
        routeData.Routers.Add(new TestRouter());
        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
        IRazorViewEngine viewEngine = services.GetRequiredService<IRazorViewEngine>();
        ITempDataProvider tempDataProvider = services.GetRequiredService<ITempDataProvider>();
        IModelMetadataProvider metadataProvider = services.GetRequiredService<IModelMetadataProvider>();
        ViewEngineResult result = viewEngine.GetView(
            executingFilePath: null,
            viewPath: "/Views/Authors/Delete.cshtml",
            isMainPage: true);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.SearchedLocations ?? []));
        var viewData = new ViewDataDictionary<Author>(metadataProvider, new ModelStateDictionary())
        {
            Model = new Author("Ada", "Lovelace", "British", new DateTime(1815, 12, 10))
        };
        using var writer = new StringWriter();
        var viewContext = new ViewContext(
            actionContext,
            result.View!,
            viewData,
            new TempDataDictionary(httpContext, tempDataProvider),
            writer,
            new HtmlHelperOptions());

        await result.View!.RenderAsync(viewContext);

        var document = await BrowsingContext.New(Configuration.Default)
            .OpenAsync(request => request.Content(writer.ToString()));
        Assert.Equal("Delete", document.QuerySelector("h1")?.TextContent.Trim());
        Assert.Contains("Ada", document.Body?.TextContent);
        Assert.NotNull(document.QuerySelector("form"));
    }

    private sealed class TestRouter : IRouter
    {
        public Task RouteAsync(RouteContext routeContext) => Task.CompletedTask;

        public VirtualPathData GetVirtualPath(VirtualPathContext context) =>
            new(this, "/Authors/Delete");
    }
}
