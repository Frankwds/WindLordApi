using FluentAssertions;
using WindLordApi.Data.Services;
using WindLordApi.Tests.Helpers;

namespace WindLordApi.Tests.Unit.Services;

public class LatestStationDataServiceTests
{
    [Fact]
    public void ConvertFromStationData_WithPartialObservation_PreservesNulls()
    {
        var stationData = new[]
        {
            TestDataBuilders.StationData()
                .WithStationId("MET-001")
                .WithWindSpeed(null)
                .WithWindGust(14.2m)
                .WithDirection(null)
                .WithTemperature(null)
                .Build()
        };

        var result = LatestStationDataService.ConvertFromStationData(stationData);

        result.Should().ContainSingle();
        result[0].StationId.Should().Be("MET-001");
        result[0].WindSpeed.Should().BeNull();
        result[0].WindGust.Should().Be(14.2m);
        result[0].Direction.Should().BeNull();
        result[0].Temperature.Should().BeNull();
    }
}