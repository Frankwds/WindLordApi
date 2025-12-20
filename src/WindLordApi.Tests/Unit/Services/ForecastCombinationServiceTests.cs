using FluentAssertions;
using WindLordApi.Integrations.MetYr;
using WindLordApi.Integrations.OpenMeteo;
using WindLordApi.Tests.Helpers;
using WindLordApi.Worker.Services;

namespace WindLordApi.Tests.Unit.Services;

/// <summary>
/// Tests for ForecastCombinationService data combination logic.
/// </summary>
public class ForecastCombinationServiceTests
{
    private readonly ForecastCombinationService _service;

    public ForecastCombinationServiceTests()
    {
        _service = new ForecastCombinationService();
    }

    #region Basic Functionality Tests

    [Fact]
    public void WithMatchingTimes_CombinesDataCorrectly()
    {
        // Arrange
        var meteoData = new[] { TestDataBuilders.OpenMeteoDto().WithTime("2024-01-01T12:00").WithTemperature2m(15.0).Build() };
        var yrData = new[] { TestDataBuilders.MetYrDto().WithTime("2024-01-01T12:00:00Z").WithAirTemperature(16.0).Build() };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsYrData.Should().BeTrue();
        result[0].Temperature.Should().Be(16.0); // Yr data takes precedence
        result[0].Time.Should().Be("2024-01-01T12:00:00Z");
    }

    [Fact]
    public void WithMultipleMatchingTimes_CombinesAllEntries()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto().WithTime("2024-01-01T12:00").Build(),
            TestDataBuilders.OpenMeteoDto().WithTime("2024-01-01T13:00").Build(),
            TestDataBuilders.OpenMeteoDto().WithTime("2024-01-01T14:00").Build()
        };
        var yrData = new[]
        {
            TestDataBuilders.MetYrDto().WithTime("2024-01-01T12:00:00Z").Build(),
            TestDataBuilders.MetYrDto().WithTime("2024-01-01T13:00:00Z").Build(),
            TestDataBuilders.MetYrDto().WithTime("2024-01-01T14:00:00Z").Build()
        };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result.Should().HaveCount(3);
        result[0].IsYrData.Should().BeTrue();
        result[1].IsYrData.Should().BeTrue();
        result[2].IsYrData.Should().BeTrue();
    }

    [Fact]
    public void WithNoMatchingTimes_UsesOnlyOpenMeteoData()
    {
        // Arrange
        var meteoData = new[] { TestDataBuilders.OpenMeteoDto().WithTime("2024-01-01T12:00").WithTemperature2m(15.0).Build() };
        var yrData = new[] { TestDataBuilders.MetYrDto().WithTime("2024-01-01T13:00:00Z").WithAirTemperature(16.0).Build() };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsYrData.Should().BeFalse();
        result[0].Temperature.Should().Be(15.0); // OpenMeteo data used
    }

    [Fact]
    public void WithEmptyMeteoData_ReturnsEmptyList()
    {
        // Arrange
        var meteoData = Array.Empty<OpenMeteoDto>();
        var yrData = new[] { TestDataBuilders.MetYrDto().Build() };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void WithEmptyYrData_UsesOnlyOpenMeteoData()
    {
        // Arrange
        var meteoData = new[] { TestDataBuilders.OpenMeteoDto().WithTime("2024-01-01T12:00").WithTemperature2m(15.0).Build() };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsYrData.Should().BeFalse();
        result[0].Temperature.Should().Be(15.0);
    }

    #endregion

    #region Time Matching Logic Tests

    [Fact]
    public void WithYrTimeWithTimezone_StripsTimezoneCorrectly()
    {
        // Arrange
        var meteoData = new[] { TestDataBuilders.OpenMeteoDto().WithTime("2024-01-01T12:00").Build() };
        var yrData = new[] { TestDataBuilders.MetYrDto().WithTime("2024-01-01T12:00:00Z").Build() };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsYrData.Should().BeTrue();
    }

    [Fact]
    public void WithYrTimeWithOffset_StripsOffsetCorrectly()
    {
        // Arrange
        var meteoData = new[] { TestDataBuilders.OpenMeteoDto().WithTime("2024-01-01T12:00").Build() };
        var yrData = new[] { TestDataBuilders.MetYrDto().WithTime("2024-01-01T12:00:00+00:00").Build() };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsYrData.Should().BeTrue();
    }

    [Fact]
    public void WithShortYrTime_HandlesGracefully()
    {
        // Arrange
        var meteoData = new[] { TestDataBuilders.OpenMeteoDto().WithTime("2024-01-01T12:00").Build() };
        var yrData = new[] { TestDataBuilders.MetYrDto().WithTime("2024-01-01T12").Build() };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result.Should().HaveCount(1);
        // Should not match since time is too short
        result[0].IsYrData.Should().BeFalse();
    }

    [Fact]
    public void WithDuplicateYrTimes_UsesFirstOccurrence()
    {
        // Arrange
        var meteoData = new[] { TestDataBuilders.OpenMeteoDto().WithTime("2024-01-01T12:00").Build() };
        var yrData = new[]
        {
            TestDataBuilders.MetYrDto().WithTime("2024-01-01T12:00:00Z").WithAirTemperature(16.0).Build(),
            TestDataBuilders.MetYrDto().WithTime("2024-01-01T12:00:00Z").WithAirTemperature(17.0).Build()
        };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result.Should().HaveCount(1);
        result[0].Temperature.Should().Be(16.0); // First occurrence used
    }

    #endregion

    #region Data Precedence Tests

    [Fact]
    public void WithYrDataAvailable_UsesYrValues()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithTemperature2m(15.0)
                .WithWindSpeed10m(10.0)
                .WithPrecipitation(0.5)
                .WithPressureMsl(1010.0)
                .WithWeatherCode("cloudy")
                .Build()
        };
        var yrData = new[]
        {
            TestDataBuilders.MetYrDto()
                .WithTime("2024-01-01T12:00:00Z")
                .WithAirTemperature(16.0)
                .WithWindSpeed(12.0)
                .WithPrecipitationAmount(0.7)
                .WithAirPressureAtSeaLevel(1012.0)
                .WithSymbolCode("partlycloudy_day")
                .Build()
        };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].Temperature.Should().Be(16.0);
        result[0].WindSpeed.Should().Be(12.0);
        result[0].Precipitation.Should().Be(0.7);
        result[0].PressureMsl.Should().Be(1012.0);
        result[0].WeatherCode.Should().Be("partlycloudy_day");
    }

    [Fact]
    public void WithYrDataNull_UsesOpenMeteoValues()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithTemperature2m(15.0)
                .WithWindSpeed10m(10.0)
                .Build()
        };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].Temperature.Should().Be(15.0);
        result[0].WindSpeed.Should().Be(10.0);
    }

    [Fact]
    public void WithYrNullableFields_UsesOpenMeteoWhenNull()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithPrecipitationProbability(30.0)
                .Build()
        };
        var yrData = new[]
        {
            TestDataBuilders.MetYrDto()
                .WithTime("2024-01-01T12:00:00Z")
                .WithProbabilityOfPrecipitation(null)
                .Build()
        };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].PrecipitationProbability.Should().Be(30.0);
    }

    #endregion

    #region IsDay Logic Tests

    [Fact]
    public void WithYrSymbolCodeContainingNight_SetsIsDayToZero()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithIsDay(1)
                .Build()
        };
        var yrData = new[]
        {
            TestDataBuilders.MetYrDto()
                .WithTime("2024-01-01T12:00:00Z")
                .WithSymbolCode("clearsky_night")
                .Build()
        };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].IsDay.Should().Be(0);
    }

    [Fact]
    public void WithYrSymbolCodeWithoutNight_UsesOpenMeteoIsDay()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithIsDay(1)
                .Build()
        };
        var yrData = new[]
        {
            TestDataBuilders.MetYrDto()
                .WithTime("2024-01-01T12:00:00Z")
                .WithSymbolCode("clearsky_day")
                .Build()
        };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].IsDay.Should().Be(1);
    }

    [Fact]
    public void WithNoYrData_UsesOpenMeteoIsDay()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithIsDay(0)
                .Build()
        };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].IsDay.Should().Be(0);
    }

    [Fact]
    public void WithYrSymbolCodeCaseInsensitive_HandlesNightCorrectly()
    {
        // Arrange
        var testCases = new[] { "NIGHT", "Night", "night", "clearsky_NIGHT" };

        foreach (var symbolCode in testCases)
        {
            var meteoData = new[]
            {
                TestDataBuilders.OpenMeteoDto()
                    .WithTime("2024-01-01T12:00")
                    .WithIsDay(1)
                    .Build()
            };
            var yrData = new[]
            {
                TestDataBuilders.MetYrDto()
                    .WithTime("2024-01-01T12:00:00Z")
                    .WithSymbolCode(symbolCode)
                    .Build()
            };

            // Act
            var result = _service.CombineDataSources(meteoData, yrData);

            // Assert
            result[0].IsDay.Should().Be(0, $"because symbol code '{symbolCode}' contains 'night'");
        }
    }

    #endregion

    #region Field Mapping Verification Tests

    [Fact]
    public void SurfaceConditions_AreMappedCorrectly()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithTemperature2m(15.0)
                .WithWindSpeed10m(10.0)
                .WithWindDirection10m(180.5)
                .WithPrecipitation(0.5)
                .WithPrecipitationProbability(30.0)
                .WithPressureMsl(1010.0)
                .WithWeatherCode("cloudy")
                .Build()
        };
        var yrData = new[]
        {
            TestDataBuilders.MetYrDto()
                .WithTime("2024-01-01T12:00:00Z")
                .WithAirTemperature(16.0)
                .WithWindSpeed(12.0)
                .WithWindFromDirection(185.7)
                .WithPrecipitationAmount(0.7)
                .WithProbabilityOfPrecipitation(40.0)
                .WithAirPressureAtSeaLevel(1012.0)
                .WithSymbolCode("partlycloudy_day")
                .WithWindSpeedOfGust(20.0)
                .WithPrecipitationAmountMax(1.0)
                .WithPrecipitationAmountMin(0.3)
                .Build()
        };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].Temperature.Should().Be(16.0);
        result[0].WindSpeed.Should().Be(12.0);
        result[0].WindDirection.Should().Be(185); // Truncated
        result[0].Precipitation.Should().Be(0.7);
        result[0].PrecipitationProbability.Should().Be(40.0);
        result[0].PressureMsl.Should().Be(1012.0);
        result[0].WeatherCode.Should().Be("partlycloudy_day");
        result[0].WindGusts.Should().Be(20.0);
        result[0].PrecipitationMax.Should().Be(1.0);
        result[0].PrecipitationMin.Should().Be(0.3);
    }

    [Fact]
    public void AtmosphericWindFields_AreMappedFromOpenMeteo()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithWindSpeed1000hPa(12.0)
                .WithWindDirection1000hPa(180.5)
                .WithWindSpeed925hPa(14.0)
                .WithWindDirection925hPa(185.7)
                .WithWindSpeed850hPa(16.0)
                .WithWindDirection850hPa(190.3)
                .WithWindSpeed700hPa(18.0)
                .WithWindDirection700hPa(195.9)
                .Build()
        };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].WindSpeed1000hpa.Should().Be(12.0);
        result[0].WindDirection1000hpa.Should().Be(180); // Truncated
        result[0].WindSpeed925hpa.Should().Be(14.0);
        result[0].WindDirection925hpa.Should().Be(185); // Truncated
        result[0].WindSpeed850hpa.Should().Be(16.0);
        result[0].WindDirection850hpa.Should().Be(190); // Truncated
        result[0].WindSpeed700hpa.Should().Be(18.0);
        result[0].WindDirection700hpa.Should().Be(195); // Truncated
    }

    [Fact]
    public void AtmosphericTemperatureFields_AreMappedFromOpenMeteo()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithTemperature1000hPa(10.0)
                .WithTemperature925hPa(8.0)
                .WithTemperature850hPa(5.0)
                .WithTemperature700hPa(0.0)
                .Build()
        };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].Temperature1000hpa.Should().Be(10.0);
        result[0].Temperature925hpa.Should().Be(8.0);
        result[0].Temperature850hpa.Should().Be(5.0);
        result[0].Temperature700hpa.Should().Be(0.0);
    }

    [Fact]
    public void CloudCoverFields_AreMappedFromOpenMeteo()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithCloudCover(50.0)
                .WithCloudCoverLow(20.0)
                .WithCloudCoverMid(15.0)
                .WithCloudCoverHigh(15.0)
                .Build()
        };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].CloudCover.Should().Be(50.0);
        result[0].CloudCoverLow.Should().Be(20.0);
        result[0].CloudCoverMid.Should().Be(15.0);
        result[0].CloudCoverHigh.Should().Be(15.0);
    }

    [Fact]
    public void StabilityFields_AreMappedFromOpenMeteo()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithCape(500.0)
                .WithConvectiveInhibition(100.0)
                .WithLiftedIndex(-2.0)
                .WithBoundaryLayerHeight(1500.0)
                .WithFreezingLevelHeight(2500.0)
                .Build()
        };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].Cape.Should().Be(500.0);
        result[0].ConvectiveInhibition.Should().Be(100.0);
        result[0].LiftedIndex.Should().Be(-2.0);
        result[0].BoundaryLayerHeight.Should().Be(1500.0);
        result[0].FreezingLevelHeight.Should().Be(2500.0);
    }

    [Fact]
    public void GeopotentialHeightFields_AreMappedFromOpenMeteo()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithGeopotentialHeight1000hPa(100.0)
                .WithGeopotentialHeight925hPa(800.0)
                .WithGeopotentialHeight850hPa(1500.0)
                .WithGeopotentialHeight700hPa(3000.0)
                .Build()
        };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].GeopotentialHeight1000hpa.Should().Be(100.0);
        result[0].GeopotentialHeight925hpa.Should().Be(800.0);
        result[0].GeopotentialHeight850hpa.Should().Be(1500.0);
        result[0].GeopotentialHeight700hpa.Should().Be(3000.0);
    }

    #endregion

    #region Wind Direction Truncation Tests

    [Fact]
    public void WithDecimalWindDirection_TruncatesCorrectly()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithWindDirection10m(180.9)
                .Build()
        };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].WindDirection.Should().Be(180); // Truncated, not rounded
    }

    [Fact]
    public void WithYrWindDirection_TruncatesCorrectly()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .Build()
        };
        var yrData = new[]
        {
            TestDataBuilders.MetYrDto()
                .WithTime("2024-01-01T12:00:00Z")
                .WithWindFromDirection(185.7)
                .Build()
        };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].WindDirection.Should().Be(185); // Truncated
    }

    [Fact]
    public void WithOpenMeteoWindDirection_TruncatesCorrectly()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithWindDirection10m(190.3)
                .Build()
        };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].WindDirection.Should().Be(190); // Truncated
    }

    #endregion

    #region Time Formatting Tests

    [Fact]
    public void WithOpenMeteoTime_AppendsTimezoneSuffix()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .Build()
        };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].Time.Should().Be("2024-01-01T12:00:00Z");
    }


    #endregion

    #region Default Values Tests

    [Fact]
    public void WithDefaultFields_SetsEmptyStrings()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .Build()
        };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].LocationId.Should().BeEmpty();
        result[0].ValidationFailures.Should().BeEmpty();
        result[0].ValidationWarnings.Should().BeEmpty();
    }

    [Fact]
    public void WithLandingFields_SetsToNull()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .Build()
        };
        var yrData = Array.Empty<MetYrDto>();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].LandingWind.Should().BeNull();
        result[0].LandingGust.Should().BeNull();
        result[0].LandingWindDirection.Should().BeNull();
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void WithLargeDatasets_ProcessesEfficiently()
    {
        // Arrange
        var meteoData = Enumerable.Range(0, 1000)
            .Select(i => TestDataBuilders.OpenMeteoDto()
                .WithTime($"2024-01-01T{i % 24:D2}:00")
                .Build())
            .ToArray();
        var yrData = Enumerable.Range(0, 1000)
            .Select(i => TestDataBuilders.MetYrDto()
                .WithTime($"2024-01-01T{i % 24:D2}:00:00Z")
                .Build())
            .ToArray();

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result.Should().HaveCount(1000);
    }

    [Fact]
    public void WithSpecialCharactersInWeatherCode_HandlesCorrectly()
    {
        // Arrange
        var meteoData = new[]
        {
            TestDataBuilders.OpenMeteoDto()
                .WithTime("2024-01-01T12:00")
                .WithWeatherCode("partly_cloudy-day")
                .Build()
        };
        var yrData = new[]
        {
            TestDataBuilders.MetYrDto()
                .WithTime("2024-01-01T12:00:00Z")
                .WithSymbolCode("partly_cloudy-day")
                .Build()
        };

        // Act
        var result = _service.CombineDataSources(meteoData, yrData);

        // Assert
        result[0].WeatherCode.Should().Be("partly_cloudy-day");
    }

    #endregion
}

