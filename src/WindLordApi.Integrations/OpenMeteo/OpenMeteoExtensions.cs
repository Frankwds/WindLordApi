using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Extension methods for registering OpenMeteo forecast API client services.
/// </summary>
public static class OpenMeteoExtensions
{
    /// <summary>
    /// Registers the OpenMeteo forecast API client services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration instance to read settings from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenMeteoClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Options (binds from appsettings)
        services.AddOptions<OpenMeteoOptions>()
            .Bind(configuration.GetSection(OpenMeteoOptions.SectionName))
            .ValidateOnStart();

        // Register HttpClient for OpenMeteoClient
        services.AddHttpClient<IOpenMeteoClient, OpenMeteoClient>();

        // Register mapping service
        services.AddScoped<IOpenMeteoMapping, OpenMeteoMappingService>();

        return services;
    }
}

