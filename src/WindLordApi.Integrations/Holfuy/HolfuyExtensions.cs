using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace WindLordApi.Integrations.Holfuy;

/// <summary>
/// Extension methods for registering Holfuy API client services
/// </summary>
public static class HolfuyExtensions
{
    /// <summary>
    /// Registers the Holfuy API client with proxy configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration instance to read settings from.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when proxy configuration is missing or invalid.</exception>
    public static IServiceCollection AddHolfuyClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Options (binds from appsettings)
        services.Configure<HolfuyOptions>(
            configuration.GetSection(HolfuyOptions.SectionName));

        // Register HttpClient + Service with proxy configuration
        services.AddHttpClient<IHolfuyClient, HolfuyClient>()
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var proxyUrl = configuration.GetConnectionString("FIXIE_URL");
                if (string.IsNullOrWhiteSpace(proxyUrl))
                {
                    throw new InvalidOperationException(
                        "FIXIE_URL connection string is not configured. Holfuy API requires a proxy connection.");
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

        return services;
    }
}

