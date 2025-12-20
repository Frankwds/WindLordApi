using Xunit;

namespace WindLordApi.Tests.Helpers;

/// <summary>
/// xUnit collection definition for PostgreSQL integration tests.
/// This ensures all tests in the collection share the same database container.
/// </summary>
[CollectionDefinition("PostgreSQL Integration Tests")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlTestContainer>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // [Collection] attributes can be derived from it.
}






