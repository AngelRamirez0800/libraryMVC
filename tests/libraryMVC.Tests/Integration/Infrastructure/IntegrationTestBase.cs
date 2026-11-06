using Xunit;

namespace libraryMVC.Tests.Integration.Infrastructure;

[Collection(IntegrationTestsGroup.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly IntegrationDatabaseFixture _fixture;

    protected IntegrationTestBase(IntegrationDatabaseFixture fixture)
    {
        _fixture = fixture;
        Client = fixture.CreateClient();
    }

    protected HttpClient Client
    {
        get;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        _fixture.ClearWebRoot();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
