using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WindLordApi.Integrations.GoogleGeocoding;

/// <summary>
/// Extension methods for registering Google Geocoding API client services.
/// </summary>
public static class GoogleGeocodingExtensions
{
    /// <summary>
    /// Registers the Google Geocoding API client services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration instance to read settings from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGoogleGeocodingClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Options (binds from appsettings) with validation
        services.AddOptions<GoogleGeocodingOptions>()
            .Bind(configuration.GetSection(GoogleGeocodingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register HttpClient for GoogleGeocodingClient
        services.AddHttpClient<IGoogleGeocodingClient, GoogleGeocodingClient>();

        return services;
    }
}
