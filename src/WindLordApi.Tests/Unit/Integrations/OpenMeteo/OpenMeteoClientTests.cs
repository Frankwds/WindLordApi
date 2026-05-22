using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WindLordApi.Integrations.OpenMeteo;

namespace WindLordApi.Tests.Unit.Integrations.OpenMeteo;

public class OpenMeteoClientTests
{
    [Fact]
    public async Task FetchForecastAsync_GivenBatchLocations_TruncatesCoordinatesAndBuildsExpectedQuery()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var client = CreateClient(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    [
                      {
                        "latitude": 60.123,
                        "longitude": 10.567,
                        "hourly": {
                          "time": [],
                          "temperature_2m": [],
                          "wind_speed_10m": [],
                          "wind_direction_10m": [],
                          "precipitation": [],
                          "precipitation_probability": [],
                          "pressure_msl": [],
                          "weather_code": [],
                          "is_day": []
                        }
                      },
                      {
                        "latitude": 61.987,
                        "longitude": 11.543,
                        "hourly": {
                          "time": [],
                          "temperature_2m": [],
                          "wind_speed_10m": [],
                          "wind_direction_10m": [],
                          "precipitation": [],
                          "precipitation_probability": [],
                          "pressure_msl": [],
                          "weather_code": [],
                          "is_day": []
                        }
                      }
                    ]
                    """,
                    Encoding.UTF8,
                    "application/json")
            },
            request => capturedRequest = request);

        // Act
        var response = await client.FetchForecastAsync(
            new[]
            {
                new OpenMeteoRequestLocation(60.1239, 10.5678),
                new OpenMeteoRequestLocation(61.9876, 11.5439)
            },
            new DateTime(2026, 5, 24, 10, 15, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 26, 10, 15, 0, DateTimeKind.Utc),
            TestContext.Current.CancellationToken);

        // Assert
        response.Should().HaveCount(2);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Contain("latitude=60.123%2C61.987");
        capturedRequest.RequestUri!.ToString().Should().Contain("longitude=10.567%2C11.543");
        capturedRequest.RequestUri!.ToString().Should().Contain("hourly=temperature_2m%2Cwind_speed_10m%2Cwind_direction_10m%2Cprecipitation%2Cprecipitation_probability%2Cpressure_msl%2Cweather_code%2Cis_day");
        capturedRequest.RequestUri!.ToString().Should().NotContain("wind_gusts_10m");
        capturedRequest.RequestUri!.ToString().Should().Contain("start_hour=2026-05-24T10%3A15");
        capturedRequest.RequestUri!.ToString().Should().Contain("wind_speed_unit=ms");
        capturedRequest.RequestUri!.ToString().Should().Contain("timezone=GMT");
    }

    [Fact]
    public async Task FetchForecastAsync_GivenSingleObjectResponse_WrapsLocationBlockInList()
    {
        // Arrange
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "latitude": 60.123,
                  "longitude": 10.567,
                  "hourly": {
                    "time": [],
                    "temperature_2m": [],
                    "wind_speed_10m": [],
                    "wind_direction_10m": [],
                    "precipitation": [],
                    "precipitation_probability": [],
                    "pressure_msl": [],
                    "weather_code": [],
                    "is_day": []
                  }
                }
                """,
                Encoding.UTF8,
                "application/json")
        });

        // Act
        var response = await client.FetchForecastAsync(
            new[] { new OpenMeteoRequestLocation(60.1234, 10.5678) },
            new DateTime(2026, 5, 24, 10, 15, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 26, 10, 15, 0, DateTimeKind.Utc),
            TestContext.Current.CancellationToken);

        // Assert
        response.Should().ContainSingle();
        response[0].Latitude.Should().Be(60.123);
        response[0].Longitude.Should().Be(10.567);
    }

    private static OpenMeteoClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        Action<HttpRequestMessage>? onRequest = null)
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            onRequest?.Invoke(request);
            return responseFactory(request);
        });

        return new OpenMeteoClient(
            new HttpClient(handler),
            Options.Create(new OpenMeteoOptions()),
            Mock.Of<ILogger<OpenMeteoClient>>());
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}