using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using WindLordApi.Data;

namespace WindLordApi.Tests.Helpers;

/// <summary>
/// Manages a PostgreSQL test container for integration tests.
/// Uses xUnit collection fixture pattern to share a single container across tests.
/// </summary>
public class PostgreSqlTestContainer : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string? _connectionString;

    public string ConnectionString => _connectionString
        ?? throw new InvalidOperationException("Container has not been initialized. Ensure InitializeAsync has been called.");

    public async ValueTask InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .Build();

        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a new ApplicationDbContext connected to the test PostgreSQL container.
    /// </summary>
    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Ensures the database schema is created by running migrations or creating tables.
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default)
    {
        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}

