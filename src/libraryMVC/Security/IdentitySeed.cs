using Microsoft.AspNetCore.Identity;

namespace libraryMVC.Security;

public static class IdentitySeed
{
    private static readonly SeedUserDefinition[] SeedUsers =
    [
        new("Admin", RoleNames.Admin, "admin", "admin@gmail.com"),
        new("Librarian", RoleNames.Librarian, "angel", "angel@gmail.com"),
        new("User", RoleNames.User, "fernando", "fernando@gmail.com")
    ];

    public static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (string roleName in new[] { RoleNames.Admin, RoleNames.Librarian, RoleNames.User })
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            IdentityResult roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            EnsureSucceeded(roleResult, $"create the '{roleName}' role");
        }
    }

    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        IConfiguration configuration)
    {
        await EnsureRolesAsync(roleManager);

        foreach (SeedUserDefinition definition in SeedUsers)
        {
            string password = configuration[$"SeedUsers:{definition.ConfigurationName}:Password"]
                ?? throw new InvalidOperationException(
                    $"No password was configured for {definition.ConfigurationName}. " +
                    $"Configure 'SeedUsers:{definition.ConfigurationName}:Password' using User Secrets or an environment variable.");
            string userName = configuration[$"SeedUsers:{definition.ConfigurationName}:UserName"]
                ?? definition.DefaultUserName;
            string email = configuration[$"SeedUsers:{definition.ConfigurationName}:Email"]
                ?? definition.DefaultEmail;

            IdentityUser? user = await userManager.FindByNameAsync(userName);
            if (user is null)
            {
                user = new IdentityUser
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true
                };

                IdentityResult userResult = await userManager.CreateAsync(user, password);
                EnsureSucceeded(userResult, $"create the user '{userName}'");
            }
            else if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                user.EmailConfirmed = true;
                IdentityResult updateResult = await userManager.UpdateAsync(user);
                EnsureSucceeded(updateResult, $"update the email for user '{userName}'");
            }

            if (!await userManager.IsInRoleAsync(user, definition.RoleName))
            {
                IdentityResult roleResult = await userManager.AddToRoleAsync(user, definition.RoleName);
                EnsureSucceeded(
                    roleResult,
                    $"assign the '{definition.RoleName}' role to the user '{userName}'");
            }
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        string errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Unable to {operation}: {errors}");
    }

    private sealed record SeedUserDefinition(
        string ConfigurationName,
        string RoleName,
        string DefaultUserName,
        string DefaultEmail);
}
