using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WindLordApi.Integrations.PortWind;

/// <summary>
/// Extension methods for registering PortWind services.
/// </summary>
public static class PortWindExtensions
{
    /// <summary>
    /// Registers the PortWind integration client and mapping services.
    /// </summary>
    public static IServiceCollection AddPortWindClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PortWindOptions>()
            .Bind(configuration.GetSection(PortWindOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IPortWindClient, PortWindClient>();
        services.AddScoped<IPortWindMapping, PortWindMappingService>();

        return services;
    }
}