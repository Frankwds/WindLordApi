using System.Text.Json;
using FluentAssertions;
using WindLordApi.Integrations.PortWind;

namespace WindLordApi.Tests.Unit.PortWind;

public class PortWindMappingTests
{
    private readonly PortWindMappingService _mapping = new();

    [Fact]
    public void NormalizeStationLabel_WithKnownMojibake_RepairsUtf8Text()
    {
        // Act
        var normalized = _mapping.NormalizeStationLabel("TromsÃ¸");

        // Assert
        normalized.Should().Be("Tromsø");
    }

    [Fact]
    public void MapStations_WithValidPayload_MapsCanonicalWeatherStationFields()
    {
        // Arrange
        var stations = new Dictionary<string, PortWindStationDto>
        {
            ["VS1285"] = new()
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["label"] = JsonDocument.Parse("\"TromsÃ¸\"").RootElement.Clone(),
                    ["location"] = JsonDocument.Parse("{\"lat\":69.6492,\"lng\":18.9553}").RootElement.Clone(),
                    ["status"] = JsonDocument.Parse("true").RootElement.Clone(),
                    ["history"] = JsonDocument.Parse("true").RootElement.Clone()
                }
            }
        };

        // Act
        var result = _mapping.MapStations(stations);

        // Assert
        result.Should().HaveCount(1);
        result[0].StationId.Should().Be("VS1285");
        result[0].Name.Should().Be("Tromsø");
        result[0].Latitude.Should().Be(69.6492m);
        result[0].Longitude.Should().Be(18.9553m);
        result[0].Altitude.Should().BeNull();
        result[0].Provider.Should().Be(PortWindOptions.ProviderName);
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public void MapStations_WithStatusOrHistoryFalse_MapsStationAsInactive()
    {
        // Arrange
        var stations = new Dictionary<string, PortWindStationDto>
        {
            ["VS1285"] = new()
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["label"] = JsonDocument.Parse("\"TromsÃ¸\"").RootElement.Clone(),
                    ["location"] = JsonDocument.Parse("{\"lat\":69.6492,\"lng\":18.9553}").RootElement.Clone(),
                    ["status"] = JsonDocument.Parse("true").RootElement.Clone(),
                    ["history"] = JsonDocument.Parse("false").RootElement.Clone()
                }
            }
        };

        // Act
        var result = _mapping.MapStations(stations);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsActive.Should().BeFalse();
    }

    [Fact]
    public void MapObservations_UsesUtsAndTemperatureAverage_AndIgnoresPreviousHelperFields()
    {
        // Arrange
        var observations = new List<PortWindObservationDto>
        {
            new()
            {
                Uts = 1716037200000,
                Properties = new Dictionary<string, JsonElement>
                {
                    ["wind_speed_avg"] = JsonDocument.Parse("8.4").RootElement.Clone(),
                    ["wind_gust"] = JsonDocument.Parse("8.9").RootElement.Clone(),
                    ["wind_speed_max"] = JsonDocument.Parse("11.1").RootElement.Clone(),
                    ["wind_speed_min"] = JsonDocument.Parse("3.2").RootElement.Clone(),
                    ["wind_direction_avg"] = JsonDocument.Parse("271").RootElement.Clone(),
                    ["temperature_avg"] = JsonDocument.Parse("13.6").RootElement.Clone(),
                    ["temperature_avg_previous"] = JsonDocument.Parse("12.9").RootElement.Clone()
                }
            }
        };

        // Act
        var result = _mapping.MapObservations("VS1285", observations);

        // Assert
        result.Should().HaveCount(1);
        result[0].StationId.Should().Be("VS1285");
        result[0].UpdatedAt.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1716037200000).UtcDateTime);
        result[0].WindSpeed.Should().Be(8.4m);
        result[0].WindGust.Should().Be(8.9m);
        result[0].WindMinSpeed.Should().Be(3.2m);
        result[0].Direction.Should().Be(271);
        result[0].Temperature.Should().Be(13.6m);
    }
}