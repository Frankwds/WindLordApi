using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WindLordApi.Integrations.PortWind;

namespace WindLordApi.Tests.Unit.Integrations.PortWind;

public class PortWindClientTests
{
    [Fact]
    public async Task FetchStationsAsync_GivenJavaScriptWrappedCatalog_ExtractsStationSet()
    {
        // Arrange
        var client = CreateClient(
            """
            window.portWindBootstrap = true;
            window.stations = {
              pw_1: {
                status: true,
                history: true,
                label: "HovdÃ¸ya   Nord",
                location: { lat: 60.12345, lng: 10.54321 }
              },
              pw_2: {
                status: false,
                history: true,
                label: "Reserve",
                location: { lat: 61.12345, lng: 11.54321 }
              }
            };
            window.renderStations();
            """);

        // Act
        var stations = await client.FetchStationsAsync(TestContext.Current.CancellationToken);

        // Assert
        stations.Should().HaveCount(2);
        stations.Should().ContainKey("pw_1");
        stations["pw_1"].Status.Should().BeTrue();
        stations["pw_1"].History.Should().BeTrue();
        stations["pw_1"].Label.Should().Be("HovdÃ¸ya   Nord");
        stations["pw_1"].Location!.Latitude.Should().Be(60.12345m);
        stations["pw_1"].Location!.Longitude.Should().Be(10.54321m);
    }

    [Fact]
    public async Task FetchStationsAsync_GivenMissingStationsAssignment_ThrowsFormatException()
    {
        // Arrange
        var client = CreateClient("window.otherStations = {};\nwindow.renderStations();");

        // Act
        var act = async () => await client.FetchStationsAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<FormatException>()
            .WithMessage("*window.stations*");
    }

    private static PortWindClient CreateClient(string responseContent)
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/javascript")
        });

        return new PortWindClient(
            new HttpClient(handler),
            Options.Create(new PortWindOptions()),
            Mock.Of<ILogger<PortWindClient>>());
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}