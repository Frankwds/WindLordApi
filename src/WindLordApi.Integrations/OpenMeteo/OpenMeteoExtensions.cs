using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WindLordApi.Integrations.OpenMeteo;

/// <summary>
/// Extension methods for registering Open-Meteo services.
/// </summary>
public static class OpenMeteoExtensions
{
    /// <summary>
    /// Registers the Open-Meteo forecast integration client and mapping services.
    /// </summary>
    public static IServiceCollection AddOpenMeteoClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<OpenMeteoOptions>()
            .Bind(configuration.GetSection(OpenMeteoOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.BaseUrl))
                {
                    options.BaseUrl = OpenMeteoOptions.DefaultBaseUrl;
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IOpenMeteoClient, OpenMeteoClient>();
        services.AddScoped<IOpenMeteoMapping, OpenMeteoMappingService>();

        return services;
    }
}