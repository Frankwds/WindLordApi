using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WindLordApi.Integrations.PortWind;

public static class PortWindExtensions
{
    public static IServiceCollection AddPortWindClient(this IServiceCollection services, IConfiguration configuration)
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