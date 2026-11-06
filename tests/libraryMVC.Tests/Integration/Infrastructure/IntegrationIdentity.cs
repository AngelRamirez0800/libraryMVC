using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using libraryMVC.Security;

namespace libraryMVC.Tests.Integration.Infrastructure;

internal static class IntegrationIdentity
{
    public static async Task<TestAccount> CreateUserAsync(
        IntegrationDatabaseFixture fixture,
        string role = RoleNames.User)
    {
        string uniqueId = Guid.NewGuid().ToString("N");
        string userName = $"integration_{uniqueId}";
        string email = $"{userName}@example.test";
        const string password = "Integration1!";

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        UserManager<IdentityUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true
        };

        IdentityResult createResult = await userManager.CreateAsync(user, password);
        EnsureSucceeded(createResult);
        IdentityResult roleResult = await userManager.AddToRoleAsync(user, role);
        EnsureSucceeded(roleResult);

        return new TestAccount(userName, email, password);
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}

internal sealed record TestAccount(string UserName, string Email, string Password);
