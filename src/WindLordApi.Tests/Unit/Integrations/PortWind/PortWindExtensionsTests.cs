using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WindLordApi.Integrations.PortWind;

namespace WindLordApi.Tests.Unit.Integrations.PortWind;

public class PortWindExtensionsTests
{
    [Fact]
    public void AddPortWindClient_GivenMissingConfiguration_UsesBuiltInDefaultUrls()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        // Act
        services.AddPortWindClient(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<PortWindOptions>>().Value;

        // Assert
        options.StationCatalogUrl.Should().Be(PortWindOptions.DefaultStationCatalogUrl);
        options.LatestDataBaseUrl.Should().Be(PortWindOptions.DefaultLatestDataBaseUrl);
    }

    [Fact]
    public void AddPortWindClient_GivenBlankUrlsInConfiguration_FallsBackToBuiltInDefaults()
    {
        // Arrange
        var settings = new Dictionary<string, string?>
        {
            [$"{PortWindOptions.SectionName}:StationCatalogUrl"] = string.Empty,
            [$"{PortWindOptions.SectionName}:LatestDataBaseUrl"] = "   "
        };

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        // Act
        services.AddPortWindClient(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<PortWindOptions>>().Value;

        // Assert
        options.StationCatalogUrl.Should().Be(PortWindOptions.DefaultStationCatalogUrl);
        options.LatestDataBaseUrl.Should().Be(PortWindOptions.DefaultLatestDataBaseUrl);
    }
}