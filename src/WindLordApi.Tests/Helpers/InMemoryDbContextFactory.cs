using Microsoft.EntityFrameworkCore;
using WindLordApi.Data;

namespace WindLordApi.Tests.Helpers;

/// <summary>
/// Factory for creating in-memory database contexts for testing.
/// </summary>
public static class InMemoryDbContextFactory
{
    /// <summary>
    /// Creates a new ApplicationDbContext with an in-memory database.
    /// </summary>
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Creates a new ApplicationDbContext with an in-memory database and seeds it with data.
    /// </summary>
    public static ApplicationDbContext CreateWithSeed(Action<ApplicationDbContext> seedAction)
    {
        var context = Create();
        seedAction(context);
        context.SaveChanges();
        return context;
    }
}

