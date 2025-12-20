using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WindLordApi.Integrations.MetYr;

/// <summary>
/// Extension methods for registering MET.no Locationforecast API (Yr) client services.
/// </summary>
public static class MetYrExtensions
{
    /// <summary>
    /// Registers the MET.no Locationforecast API (Yr) client services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration instance to read settings from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMetYrClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Options (binds from appsettings)
        services.AddOptions<MetYrOptions>()
            .Bind(configuration.GetSection(MetYrOptions.SectionName))
            .ValidateOnStart();

        // Register HttpClient for MetYrClient
        services.AddHttpClient<IMetYrClient, MetYrClient>();

        // Register mapping service
        services.AddScoped<IMetYrMapping, MetYrMappingService>();

        return services;
    }
}

