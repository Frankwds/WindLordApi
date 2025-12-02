using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WindLordApi.Data;

/// <summary>
/// Design-time factory for creating ApplicationDbContext instances during migrations.
/// This allows EF Core tools to create the DbContext without requiring a running application.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings (looks in Worker project)
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "WindLordApi.Worker"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets("dotnet-WindLordApi.Worker-23a36c7b-d3ab-4b3a-97ed-1112a525033a")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("SUPABASE_CONNECTION_STRING")
            ?? throw new InvalidOperationException("SUPABASE_CONNECTION_STRING not found in configuration");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

