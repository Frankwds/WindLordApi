using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WindLordApi.Integrations.WindsMobi;

/// <summary>
/// Extension methods for registering WindsMobi API client services.
/// </summary>
public static class WindsMobiExtensions
{
    /// <summary>
    /// Registers the WindsMobi API client services.
    /// No credentials required - WindsMobi is a free, community-driven API.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration instance to read settings from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWindsMobiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register HttpClient for WindsMobiClient
        services.AddHttpClient<IWindsMobiClient, WindsMobiClient>();

        // Register mapping service
        services.AddScoped<IWindsMobiMapping, WindsMobiMappingService>();

        return services;
    }
}
