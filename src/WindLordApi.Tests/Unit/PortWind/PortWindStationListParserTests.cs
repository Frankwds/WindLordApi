using FluentAssertions;
using WindLordApi.Integrations.PortWind;

namespace WindLordApi.Tests.Unit.PortWind;

public class PortWindStationListParserTests
{
    [Fact]
    public void ExtractJsonObject_WithTrailingJavaScriptAndUnquotedKeys_ReturnsDeserializableJson()
    {
        // Arrange
        const string payload = """
window.stations = {
  VS1285: {
    label: \"TromsÃ¸\",
    lat: 69.6492,
    lng: 18.9553,
    status: false,
    maintenance: true,
    camera: { enabled: true },
    sensors: ['wind'],
  },
  VS2000: {
    label: 'BodÃ¸',
    latitude: 67.2804,
    longitude: 14.4049
  }
};
window.renderStations();
""";

        // Act
        var json = PortWindStationListParser.ExtractJsonObject(payload);

        // Assert
        json.Should().Contain("\"VS1285\"");
        json.Should().Contain("\"label\"");
        json.Should().Contain("\"BodÃ¸\"");
        json.Should().NotContain("window.renderStations");
    }

    [Fact]
    public void ExtractJsonObject_WithoutWindowStationsAssignment_ThrowsFormatException()
    {
        // Act
        var act = () => PortWindStationListParser.ExtractJsonObject("window.other = {};");

        // Assert
        act.Should().Throw<FormatException>();
    }
}