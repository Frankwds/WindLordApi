using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WindLordApi.Data.Extensions;

/// <summary>
/// Extension methods for configuration to simplify connection string retrieval.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Gets the Supabase connection string based on the current environment.
    /// Uses Production connection string when in Production environment, otherwise uses Development connection string.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="environment">Optional host environment. If provided, uses IsProduction() to determine the connection string.</param>
    /// <returns>The connection string for the current environment.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the connection string is not found.</exception>
    public static string GetSupabaseConnectionString(
        this IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        var isProduction = environment?.IsProduction()
            ?? IsProductionEnvironment();

        var connectionStringKey = isProduction
            ? "SUPABASE_CONNECTION_STRING_PRODUCTION"
            : "SUPABASE_CONNECTION_STRING";

        var environmentName = environment?.EnvironmentName
            ?? GetEnvironmentName();

        var connectionString = configuration.GetConnectionString(connectionStringKey);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionStringKey}' not found. " +
                $"Current environment: {environmentName}");
        }

        return connectionString;
    }

    /// <summary>
    /// Determines if the current environment is Production by checking environment variables.
    /// </summary>
    private static bool IsProductionEnvironment()
    {
        var environment = GetEnvironmentName();
        return string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the current environment name from environment variables.
    /// Checks DOTNET_ENVIRONMENT first, then ASPNETCORE_ENVIRONMENT, defaults to "Development".
    /// </summary>
    private static string GetEnvironmentName()
    {
        return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";
    }
}

