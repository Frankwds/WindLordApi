using FluentAssertions;
using WindLordApi.Integrations.PortWind;

namespace WindLordApi.Tests.Unit.Integrations.PortWind;

public class PortWindMappingTests
{
    private readonly PortWindMappingService _mapping = new();

    [Fact]
    public void MapToStationRefreshResult_GivenMojibakeAndProviderState_MapsStationsAndStatusBuckets()
    {
        // Arrange
        var stations = new Dictionary<string, PortWindStationCatalogEntry>
        {
            ["pw-1"] = new()
            {
                Status = true,
                History = true,
                Label = "HovdÃ¸ya   Nord",
                Location = new PortWindStationLocation { Latitude = 60.123456m, Longitude = 10.654321m }
            },
            ["pw-2"] = new()
            {
                Status = true,
                History = false,
                Label = "Reserve",
                Location = new PortWindStationLocation { Latitude = 59.987654m, Longitude = 11.123456m }
            },
            ["pw-3"] = new()
            {
                Status = true,
                History = true,
                Label = "Invalid",
                Location = new PortWindStationLocation { Latitude = 0m, Longitude = 11.123456m }
            }
        };

        // Act
        var result = _mapping.MapToStationRefreshResult(stations);

        // Assert
        result.WeatherStations.Should().HaveCount(2);
        result.SeenStationIds.Should().Equal("pw-1", "pw-2");
        result.ActiveStationIds.Should().Equal("pw-1");
        result.InactiveStationIds.Should().Equal("pw-2");

        var activeStation = result.WeatherStations.Should().ContainSingle(station => station.StationId == "pw-1").Which;
        activeStation.Name.Should().Be("Hovdøya Nord");
        activeStation.IsActive.Should().BeTrue();
        activeStation.Provider.Should().Be(PortWindOptions.ProviderName);
        activeStation.Latitude.Should().Be(60.12346m);
        activeStation.Longitude.Should().Be(10.65432m);
    }

    [Fact]
    public void MapToStationData_GivenLatestObservation_UsesLastMeasurementAndFallsBackToMaxWind()
    {
        // Arrange
        var lastMeasurement = 1732968000000L;
        var response = new PortWindLatestResponse
        {
            LastMeasurement = lastMeasurement,
            Data = new[]
            {
                new PortWindLatestDataPoint
                {
                    WindSpeedAverage = 6.4m,
                    WindDirectionAverage = 361m,
                    WindSpeedMax = 10.2m,
                    TemperatureAverage = 4.5m
                }
            }
        };

        // Act
        var stationData = _mapping.MapToStationData("pw-1", response);

        // Assert
        stationData.Should().NotBeNull();
        stationData!.StationId.Should().Be("pw-1");
        stationData.UpdatedAt.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(lastMeasurement).UtcDateTime);
        stationData.Direction.Should().Be(1);
        stationData.WindSpeed.Should().Be(6.4m);
        stationData.WindGust.Should().Be(10.2m);
        stationData.Temperature.Should().Be(4.5m);
        stationData.IsCompressed.Should().BeFalse();
    }
}