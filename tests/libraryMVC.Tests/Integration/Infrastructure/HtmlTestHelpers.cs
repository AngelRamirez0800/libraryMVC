using AngleSharp;
using AngleSharp.Dom;

namespace libraryMVC.Tests.Integration.Infrastructure;

internal static class HtmlTestHelpers
{
    public static async Task<IDocument> GetDocumentAsync(HttpClient client, string path)
    {
        using HttpResponseMessage response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return await ParseAsync(response);
    }

    public static async Task<IDocument> ParseAsync(HttpResponseMessage response)
    {
        string html = await response.Content.ReadAsStringAsync();
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(request => request.Content(html));
    }

    public static string GetAntiforgeryToken(IDocument document)
    {
        return document.QuerySelector("input[name='__RequestVerificationToken']")
            ?.GetAttribute("value")
            ?? throw new InvalidOperationException("The response did not contain an antiforgery token.");
    }

    public static string GetRedirectPathAndQuery(HttpResponseMessage response)
    {
        Uri location = response.Headers.Location
            ?? throw new InvalidOperationException("The response did not contain a redirect location.");
        return location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
    }
}
