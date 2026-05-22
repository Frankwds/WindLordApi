using FluentAssertions;
using WindLordApi.Integrations.OpenMeteo;

namespace WindLordApi.Tests.Unit.Integrations.OpenMeteo;

public class OpenMeteoMappingTests
{
    private readonly OpenMeteoMappingService _mapping = new();

    [Fact]
    public void MapForecasts_GivenUnsupportedWeatherCode_LeavesWeatherCodeUnset()
    {
        // Arrange
        var responses = new[]
        {
            new OpenMeteoForecastResponse
            {
                Latitude = 60.123,
                Longitude = 10.567,
                Hourly = new OpenMeteoHourlyForecast
                {
                    Time = new[] { "2026-05-24T10:00" },
                    Temperature2m = new double?[] { 12.3 },
                    WindSpeed10m = new double?[] { 6.1 },
                    WindDirection10m = new double?[] { 180 },
                    Precipitation = new double?[] { 0.2 },
                    PrecipitationProbability = new double?[] { 35 },
                    PressureMsl = new double?[] { 1010.1 },
                    WeatherCode = new int?[] { 123 },
                    IsDay = new int?[] { 1 }
                }
            }
        };

        // Act
        var result = _mapping.MapForecasts(responses);

        // Assert
        result.Should().ContainSingle();
        result[0].Forecasts.Should().ContainSingle();
        result[0].Forecasts[0].WeatherCode.Should().BeNull();
    }

    [Fact]
    public void MapForecasts_GivenNumericValues_RoundsToDestinationPrecision()
    {
        // Arrange
        var responses = new[]
        {
            new OpenMeteoForecastResponse
            {
                Latitude = 60.123,
                Longitude = 10.567,
                Hourly = new OpenMeteoHourlyForecast
                {
                    Time = new[] { "2026-05-24T10:00" },
                    Temperature2m = new double?[] { 12.34 },
                    WindSpeed10m = new double?[] { 6.16 },
                    WindDirection10m = new double?[] { 180.6 },
                    Precipitation = new double?[] { 0.256 },
                    PrecipitationProbability = new double?[] { 35.556 },
                    PressureMsl = new double?[] { 1010.16 },
                    WeatherCode = new int?[] { 1 },
                    IsDay = new int?[] { 0 }
                }
            }
        };

        // Act
        var result = _mapping.MapForecasts(responses);

        // Assert
        var forecast = result[0].Forecasts[0];
        forecast.Temperature.Should().Be(12.3m);
        forecast.WindSpeed.Should().Be(6.2m);
        forecast.WindDirection.Should().Be(181);
        forecast.WindGusts.Should().BeNull();
        forecast.Precipitation.Should().Be(0.26m);
        forecast.PrecipitationProbability.Should().BeApproximately(35.56f, 0.001f);
        forecast.PressureMsl.Should().Be(1010.2m);
        forecast.WeatherCode.Should().Be("partlycloudy_night");
        forecast.IsDay.Should().Be(0);
    }

    [Fact]
    public void MapForecasts_GivenMismatchedHourlyArrayLengths_ThrowsInvalidOperationException()
    {
        // Arrange
        var responses = new[]
        {
            new OpenMeteoForecastResponse
            {
                Latitude = 60.123,
                Longitude = 10.567,
                Hourly = new OpenMeteoHourlyForecast
                {
                    Time = new[] { "2026-05-24T10:00", "2026-05-24T11:00" },
                    Temperature2m = new double?[] { 12.3 },
                    WindSpeed10m = new double?[] { 6.1, 6.2 },
                    WindDirection10m = new double?[] { 180, 181 },
                    Precipitation = new double?[] { 0.2, 0.3 },
                    PrecipitationProbability = new double?[] { 35, 40 },
                    PressureMsl = new double?[] { 1010.1, 1010.2 },
                    WeatherCode = new int?[] { 1, 2 },
                    IsDay = new int?[] { 1, 1 }
                }
            }
        };

        // Act
        var act = () => _mapping.MapForecasts(responses);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Temperature2m*");
    }
}