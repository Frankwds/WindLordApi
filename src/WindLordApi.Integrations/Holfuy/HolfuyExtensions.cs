using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;

namespace WindLordApi.Integrations.Holfuy;

/// <summary>
/// Extension methods for registering Holfuy API client services
/// </summary>
public static class HolfuyExtensions
{
    /// <summary>
    /// Registers the Holfuy API client with conditional proxy configuration.
    /// Uses proxy when IS_LOCAL=true, connects directly when IS_LOCAL=false or not set.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration instance to read settings from.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when proxy configuration is missing or invalid (only when IS_LOCAL=true).</exception>
    public static IServiceCollection AddHolfuyClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Options (binds from appsettings) with validation
        services.AddOptions<HolfuyOptions>()
            .Bind(configuration.GetSection(HolfuyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Check IS_LOCAL environment variable (defaults to false if not set)
        var isLocalValue = configuration["IS_LOCAL"];
        var isLocal = !string.IsNullOrWhiteSpace(isLocalValue) &&
                     (isLocalValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                      isLocalValue == "1");

        var httpClientBuilder = services.AddHttpClient<IHolfuyClient, HolfuyClient>();

        // Only configure proxy when IS_LOCAL=true
        if (isLocal)
        {
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
            {
                var proxyUrl = configuration.GetConnectionString("FIXIE_URL");
                if (string.IsNullOrWhiteSpace(proxyUrl))
                {
                    throw new InvalidOperationException(
                        "FIXIE_URL connection string is not configured. Holfuy API requires a proxy connection when IS_LOCAL=true.");
                }

                var proxyUri = new Uri(proxyUrl);
                if (string.IsNullOrWhiteSpace(proxyUri.Host) || proxyUri.Port == -1)
                {
                    throw new InvalidOperationException("Invalid proxy URL: missing hostname or port");
                }

                var credentials = proxyUri.UserInfo;
                if (string.IsNullOrWhiteSpace(credentials))
                {
                    throw new InvalidOperationException("Invalid proxy URL: missing authentication credentials");
                }

                var credentialParts = credentials.Split(':');
                if (credentialParts.Length != 2)
                {
                    throw new InvalidOperationException(
                        "Invalid proxy URL: authentication credentials must be in format username:password");
                }

                var proxy = new WebProxy
                {
                    Address = new Uri($"http://{proxyUri.Host}:{proxyUri.Port}"),
                    Credentials = new NetworkCredential(credentialParts[0], credentialParts[1])
                };

                return new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = true
                };
            });
        }

        return services;
    }
}

