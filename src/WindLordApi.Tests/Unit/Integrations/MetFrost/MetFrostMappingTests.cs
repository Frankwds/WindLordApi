using FluentAssertions;
using WindLordApi.Integrations.MetFrost;

namespace WindLordApi.Tests.Unit.Integrations.MetFrost;

public class MetFrostMappingTests
{
    private readonly MetFrostMappingService _service = new();

    [Fact]
    public void MapMetObservationsToStationData_WithOnlyWindGust_PersistsPartialObservation()
    {
        var observations = new[]
        {
            CreateDataPoint(
                "SN12345:0",
                new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero),
                CreateObservation("max(wind_speed_of_gust PT10M)", 12.4))
        };

        var result = _service.MapMetObservationsToStationData(observations);

        result.Should().ContainSingle();
        var stationData = result[0];
        stationData.StationId.Should().Be("SN12345");
        stationData.WindGust.Should().Be(12.4m);
        stationData.WindSpeed.Should().BeNull();
        stationData.Direction.Should().BeNull();
        stationData.Temperature.Should().BeNull();
    }

    [Fact]
    public void MapMetObservationsToStationData_WithNoMappedValues_DropsObservation()
    {
        var observations = new[]
        {
            CreateDataPoint(
                "SN12345:0",
                new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero),
                CreateObservation("relative_humidity", 80.0))
        };

        var result = _service.MapMetObservationsToStationData(observations);

        result.Should().BeEmpty();
    }

    [Fact]
    public void MapMetObservationsToStationData_WithDirectionOnly_DropsObservation()
    {
        var observations = new[]
        {
            CreateDataPoint(
                "SN12345:0",
                new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero),
                CreateObservation("wind_from_direction", 361.0))
        };

        var result = _service.MapMetObservationsToStationData(observations);

        result.Should().BeEmpty();
    }

    [Fact]
    public void MapMetObservationsToStationData_WithTemperatureOnly_DropsObservation()
    {
        var observations = new[]
        {
            CreateDataPoint(
                "SN12345:0",
                new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero),
                CreateObservation("air_temperature", 18.5))
        };

        var result = _service.MapMetObservationsToStationData(observations);

        result.Should().BeEmpty();
    }

    private static MetObservationsData CreateDataPoint(
        string sourceId,
        DateTimeOffset referenceTime,
        params MetObservation[] observations)
    {
        return new MetObservationsData
        {
            SourceId = sourceId,
            ReferenceTime = referenceTime,
            Observations = observations
        };
    }

    private static MetObservation CreateObservation(string elementId, double value)
    {
        return new MetObservation
        {
            ElementId = elementId,
            Value = value
        };
    }
}