using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Data;
using WindLordApi.Data.Models;
using WindLordApi.Data.Repositories;
using WindLordApi.Tests.Helpers;
using Xunit;

namespace WindLordApi.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for WeatherStationRepository methods that use ExecuteUpdateAsync.
/// These tests use a real PostgreSQL database via Testcontainers to verify the actual
/// production code path, since ExecuteUpdateAsync is not supported by in-memory database.
/// </summary>
[Collection("PostgreSQL Integration Tests")]
public class WeatherStationRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlTestContainer _container;
    private ApplicationDbContext _context = null!;
    private WeatherStationRepository _repository = null!;
    private readonly Mock<ILogger<WeatherStationRepository>> _loggerMock;

    public WeatherStationRepositoryIntegrationTests(PostgreSqlTestContainer container)
    {
        _container = container;
        _loggerMock = new Mock<ILogger<WeatherStationRepository>>();
    }

    public async ValueTask InitializeAsync()
    {
        // Ensure database schema is created
        await _container.EnsureDatabaseCreatedAsync();

        // Create a fresh context for each test
        _context = _container.CreateDbContext();
        _repository = new WeatherStationRepository(_context, _loggerMock.Object);

        // Clean up any existing data
        _context.WeatherStations.RemoveRange(_context.WeatherStations);
        _context.StationData.RemoveRange(_context.StationData);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up test data after each test
        if (_context != null)
        {
            _context.WeatherStations.RemoveRange(_context.WeatherStations);
            _context.StationData.RemoveRange(_context.StationData);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            await _context.DisposeAsync();
        }
    }

    [Fact]
    public async Task SetAllStationsWithDataToActiveByProviderAsync_WithInactiveStationsHavingData_ActivatesStations()
    {
        // Arrange
        var inactiveStationWithData = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(false)
            .Build();
        var inactiveStationWithoutData = TestDataBuilders.WeatherStation()
            .WithStationId("MET-002")
            .WithProvider("MET")
            .WithIsActive(false)
            .Build();
        var activeStationWithData = TestDataBuilders.WeatherStation()
            .WithStationId("MET-003")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();

        _context.WeatherStations.AddRange(
            inactiveStationWithData, inactiveStationWithoutData, activeStationWithData);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add station data for MET-001 and MET-003
        var stationData1 = TestDataBuilders.StationData()
            .WithStationId("MET-001")
            .Build();
        var stationData3 = TestDataBuilders.StationData()
            .WithStationId("MET-003")
            .Build();
        _context.StationData.AddRange(stationData1, stationData3);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.SetAllStationsWithDataToActiveByProviderAsync("MET", TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1); // Only MET-001 should be activated (MET-003 is already active)

        // Refresh context to see changes
        await _context.Entry(inactiveStationWithData).ReloadAsync(TestContext.Current.CancellationToken);
        await _context.Entry(inactiveStationWithoutData).ReloadAsync(TestContext.Current.CancellationToken);
        await _context.Entry(activeStationWithData).ReloadAsync(TestContext.Current.CancellationToken);

        inactiveStationWithData.IsActive.Should().BeTrue();
        inactiveStationWithoutData.IsActive.Should().BeFalse();
        activeStationWithData.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SetAllStationsWithDataToActiveByProviderAsync_WithNoInactiveStationsHavingData_ReturnsZero()
    {
        // Arrange
        var inactiveStationWithoutData = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(false)
            .Build();
        await _context.WeatherStations.AddAsync(inactiveStationWithoutData, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.SetAllStationsWithDataToActiveByProviderAsync("MET", TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task SetAllStationsWithoutDataToInactiveByProviderAsync_WithActiveStationsWithoutData_DeactivatesStations()
    {
        // Arrange
        var activeStationWithoutData = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();
        var activeStationWithData = TestDataBuilders.WeatherStation()
            .WithStationId("MET-002")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();
        var inactiveStationWithoutData = TestDataBuilders.WeatherStation()
            .WithStationId("MET-003")
            .WithProvider("MET")
            .WithIsActive(false)
            .Build();

        _context.WeatherStations.AddRange(
            activeStationWithoutData, activeStationWithData, inactiveStationWithoutData);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add station data only for MET-002
        var stationData = TestDataBuilders.StationData()
            .WithStationId("MET-002")
            .Build();
        await _context.StationData.AddAsync(stationData, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.SetAllStationsWithoutDataToInactiveByProviderAsync("MET", TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1); // Only MET-001 should be deactivated

        // Refresh context to see changes
        await _context.Entry(activeStationWithoutData).ReloadAsync(TestContext.Current.CancellationToken);
        await _context.Entry(activeStationWithData).ReloadAsync(TestContext.Current.CancellationToken);
        await _context.Entry(inactiveStationWithoutData).ReloadAsync(TestContext.Current.CancellationToken);

        activeStationWithoutData.IsActive.Should().BeFalse();
        activeStationWithData.IsActive.Should().BeTrue();
        inactiveStationWithoutData.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SetAllStationsWithoutDataToInactiveByProviderAsync_WithNoActiveStationsWithoutData_ReturnsZero()
    {
        // Arrange
        var activeStationWithData = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();

        var activeStationWithData2 = TestDataBuilders.WeatherStation()
            .WithStationId("MET-002")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();
        _context.WeatherStations.AddRange(activeStationWithData, activeStationWithData2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var stationData = TestDataBuilders.StationData()
            .WithStationId("MET-001")
            .Build();
        var stationData2 = TestDataBuilders.StationData()
            .WithStationId("MET-002")
            .Build();
        _context.StationData.AddRange(stationData, stationData2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.SetAllStationsWithoutDataToInactiveByProviderAsync("MET", TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task SetStationsInactiveByProviderAsync_OnlyDeactivatesStationsForRequestedProvider()
    {
        // Arrange
        var activeMetStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();
        var activePortWindStation = TestDataBuilders.WeatherStation()
            .WithStationId("PORTWIND-001")
            .WithProvider("PortWind")
            .WithIsActive(true)
            .Build();

        _context.WeatherStations.AddRange(activeMetStation, activePortWindStation);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.SetStationsInactiveByProviderAsync("MET", ["MET-001", "PORTWIND-001"], TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);

        await _context.Entry(activeMetStation).ReloadAsync(TestContext.Current.CancellationToken);
        await _context.Entry(activePortWindStation).ReloadAsync(TestContext.Current.CancellationToken);

        activeMetStation.IsActive.Should().BeFalse();
        activePortWindStation.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SetMissingStationsInactiveByProviderAsync_OnlyDeactivatesMissingStationsForRequestedProvider()
    {
        // Arrange
        var seenPortWindStation = TestDataBuilders.WeatherStation()
            .WithStationId("VS1285")
            .WithProvider("PortWind")
            .WithIsActive(true)
            .Build();
        var missingPortWindStation = TestDataBuilders.WeatherStation()
            .WithStationId("VS1286")
            .WithProvider("PortWind")
            .WithIsActive(true)
            .Build();
        var activeMetStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();

        _context.WeatherStations.AddRange(seenPortWindStation, missingPortWindStation, activeMetStation);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.SetMissingStationsInactiveByProviderAsync("PortWind", ["VS1285"], TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);

        await _context.Entry(seenPortWindStation).ReloadAsync(TestContext.Current.CancellationToken);
        await _context.Entry(missingPortWindStation).ReloadAsync(TestContext.Current.CancellationToken);
        await _context.Entry(activeMetStation).ReloadAsync(TestContext.Current.CancellationToken);

        seenPortWindStation.IsActive.Should().BeTrue();
        missingPortWindStation.IsActive.Should().BeFalse();
        activeMetStation.IsActive.Should().BeTrue();

        // Assert
        result.Should().Be(0);
    }
}

