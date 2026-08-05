using Xunit;

namespace SCPM.IntegrationTests;

/// <summary>Shares one ScpmWebApplicationFactory (and its one-time DB reset) across every test
/// class in this assembly — xunit runs classes within a collection sequentially, so tests don't
/// race on the same database.</summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<ScpmWebApplicationFactory>
{
    public const string Name = "Integration";
}
