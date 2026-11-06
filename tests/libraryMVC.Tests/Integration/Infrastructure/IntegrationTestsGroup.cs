using Xunit;

namespace libraryMVC.Tests.Integration.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestsGroup : ICollectionFixture<IntegrationDatabaseFixture>
{
    public const string Name = "SQL Server integration";
}
