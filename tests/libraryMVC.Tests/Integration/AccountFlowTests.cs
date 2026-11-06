using System.Net;
using AngleSharp.Dom;
using Xunit;
using libraryMVC.Tests.Integration.Infrastructure;

namespace libraryMVC.Tests.Integration;

public sealed class AccountFlowTests : IntegrationTestBase
{
    private readonly IntegrationDatabaseFixture _fixture;

    public AccountFlowTests(IntegrationDatabaseFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RegistrationLoginAndLogoutUpdateAuthenticatedHtmlState()
    {
        string userName = $"registered_{Guid.NewGuid():N}";
        string email = $"{userName}@example.test";
        const string password = "Registration1!";

        IDocument registerPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Account/Register");
        string registerToken = HtmlTestHelpers.GetAntiforgeryToken(registerPage);
        using HttpResponseMessage registration = await Client.PostAsync(
            "/Account/Register",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = registerToken,
                ["UserName"] = userName,
                ["Email"] = email,
                ["Password"] = password,
                ["ConfirmPassword"] = password
            }));

        Assert.Equal(HttpStatusCode.Redirect, registration.StatusCode);
        IDocument signedInHome = await HtmlTestHelpers.GetDocumentAsync(Client, "/");
        Assert.Contains(userName, signedInHome.QuerySelector("nav")?.TextContent);
        Assert.NotNull(signedInHome.QuerySelector("form[action='/Account/Logout']"));

        string logoutToken = HtmlTestHelpers.GetAntiforgeryToken(signedInHome);
        using HttpResponseMessage logout = await Client.PostAsync(
            "/Account/Logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = logoutToken
            }));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);

        IDocument loginPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Account/Login");
        string loginToken = HtmlTestHelpers.GetAntiforgeryToken(loginPage);
        using HttpResponseMessage login = await Client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = loginToken,
                ["UserNameOrEmail"] = email,
                ["Password"] = password,
                ["RememberMe"] = "false",
                ["ReturnUrl"] = "/Home/AcercaDe"
            }));

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/Home/AcercaDe", HtmlTestHelpers.GetRedirectPathAndQuery(login));
        IDocument signedInPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Home/AcercaDe");
        Assert.Contains(userName, signedInPage.QuerySelector("nav")?.TextContent);

        string logoutTokenAfterLogin = HtmlTestHelpers.GetAntiforgeryToken(signedInPage);
        using HttpResponseMessage finalLogout = await Client.PostAsync(
            "/Account/Logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = logoutTokenAfterLogin
            }));
        Assert.Equal(HttpStatusCode.Redirect, finalLogout.StatusCode);

        using HttpResponseMessage protectedResponse = await Client.GetAsync("/Home/AcercaDe");
        Assert.Equal(HttpStatusCode.Redirect, protectedResponse.StatusCode);
        Assert.StartsWith("/Account/Login", HtmlTestHelpers.GetRedirectPathAndQuery(protectedResponse));
    }

    [Fact]
    public async Task RepeatedBadPasswordsLockAccountAndRejectsCorrectPassword()
    {
        TestAccount account = await IntegrationIdentity.CreateUserAsync(_fixture);
        IDocument loginPage = await HtmlTestHelpers.GetDocumentAsync(Client, "/Account/Login");
        string token = HtmlTestHelpers.GetAntiforgeryToken(loginPage);

        HttpResponseMessage? lastFailure = null;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            lastFailure?.Dispose();
            lastFailure = await Client.PostAsync(
                "/Account/Login",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["UserNameOrEmail"] = account.UserName,
                    ["Password"] = "Wrong-password1!",
                    ["RememberMe"] = "false"
                }));
            Assert.Equal(HttpStatusCode.OK, lastFailure.StatusCode);
        }

        HttpResponseMessage lockedResponse = lastFailure
            ?? throw new InvalidOperationException("No lockout response was produced.");
        using (lockedResponse)
        {
            IDocument lockedPage = await HtmlTestHelpers.ParseAsync(lockedResponse);
            Assert.Contains("temporarily locked", lockedPage.Body?.TextContent);
        }

        using HttpResponseMessage correctPassword = await Client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["UserNameOrEmail"] = account.UserName,
                ["Password"] = account.Password,
                ["RememberMe"] = "false"
            }));
        Assert.Equal(HttpStatusCode.OK, correctPassword.StatusCode);
        IDocument stillLockedPage = await HtmlTestHelpers.ParseAsync(correctPassword);
        Assert.Contains("temporarily locked", stillLockedPage.Body?.TextContent);
    }
}
