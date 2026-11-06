using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using libraryMVC.Data;
using libraryMVC.Security;
using Respawn;
using Testcontainers.MsSql;
using Xunit;

namespace libraryMVC.Tests.Integration.Infrastructure;

public sealed class IntegrationDatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword($"Aa1!{Guid.NewGuid():N}")
        .Build();

    private Respawner? _respawner;
    private string _connectionString = string.Empty;
    private readonly string _webRootPath = Path.Combine(Path.GetTempPath(), $"library-mvc-tests-{Guid.NewGuid():N}");

    public LibraryWebApplicationFactory Factory { get; private set; } = null!;
    public string WebRootPath => _webRootPath;

    public async ValueTask InitializeAsync()
    {
        await _sqlServer.StartAsync();
        string databaseName = $"LibraryMvcTests_{Guid.NewGuid():N}";
        SqlConnectionStringBuilder serverConnectionString = new(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "master"
        };
        await using (var serverConnection = new SqlConnection(serverConnectionString.ConnectionString))
        {
            await serverConnection.OpenAsync();
            await using var createDatabase = serverConnection.CreateCommand();
            createDatabase.CommandText = $"CREATE DATABASE [{databaseName}]";
            await createDatabase.ExecuteNonQueryAsync();
        }

        _connectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = databaseName
        }.ConnectionString;

        Directory.CreateDirectory(_webRootPath);
        Factory = new LibraryWebApplicationFactory(_connectionString, _webRootPath);
        using HttpClient client = CreateClient();

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(databaseName, context.Database.GetDbConnection().Database);
            await context.Database.MigrateAsync();
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    public HttpClient CreateClient()
    {
        return Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task ResetDatabaseAsync()
    {
        ClearWebRoot();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);

        await using var scope = Factory.Services.CreateAsyncScope();
        RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await IdentitySeed.EnsureRolesAsync(roleManager);

        await using var seedScope = Factory.Services.CreateAsyncScope();
        ApplicationDbContext context = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await LibraryDatabaseSeeder.SeedAsync(context, _webRootPath);
    }

    public void ClearWebRoot()
    {
        foreach (string filePath in Directory.EnumerateFiles(_webRootPath))
        {
            File.Delete(filePath);
        }

        foreach (string directoryPath in Directory.EnumerateDirectories(_webRootPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Factory.Dispose();
        await _sqlServer.DisposeAsync();
        if (Directory.Exists(_webRootPath))
        {
            Directory.Delete(_webRootPath, recursive: true);
        }
    }
}
