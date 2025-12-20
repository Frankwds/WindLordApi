using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Data;
using WindLordApi.Data.Models;
using WindLordApi.Data.Repositories;
using WindLordApi.Tests.Helpers;
using Xunit;

namespace WindLordApi.Tests.Unit.Repositories;

/// <summary>
/// Tests for WeatherStationRepository custom query methods.
/// </summary>
public class WeatherStationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<WeatherStationRepository>> _loggerMock;
    private readonly WeatherStationRepository _repository;

    public WeatherStationRepositoryTests()
    {
        _context = InMemoryDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<WeatherStationRepository>>();
        _repository = new WeatherStationRepository(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task GetActiveMETStationIdsAsync_WithActiveMETStations_ReturnsStationIds()
    {
        // Arrange
        var activeMetStation1 = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();
        var activeMetStation2 = TestDataBuilders.WeatherStation()
            .WithStationId("MET-002")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();
        var inactiveMetStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-003")
            .WithProvider("MET")
            .WithIsActive(false)
            .Build();
        var activeHolfuyStation = TestDataBuilders.WeatherStation()
            .WithStationId("HOLFUY-001")
            .WithProvider("HOLFUY")
            .WithIsActive(true)
            .Build();

        _context.WeatherStations.AddRange(
            activeMetStation1, activeMetStation2, inactiveMetStation, activeHolfuyStation);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetActiveMETStationIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("MET-001");
        result.Should().Contain("MET-002");
        result.Should().NotContain("MET-003");
        result.Should().NotContain("HOLFUY-001");
    }

    [Fact]
    public async Task GetActiveMETStationIdsAsync_WithNoActiveMETStations_ReturnsEmptyCollection()
    {
        // Arrange
        var inactiveMetStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(false)
            .Build();
        await _context.WeatherStations.AddAsync(inactiveMetStation, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetActiveMETStationIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInactiveMETStationIdsAsync_WithInactiveMETStations_ReturnsStationIds()
    {
        // Arrange
        var inactiveMetStation1 = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(false)
            .Build();
        var inactiveMetStation2 = TestDataBuilders.WeatherStation()
            .WithStationId("MET-002")
            .WithProvider("MET")
            .WithIsActive(false)
            .Build();
        var activeMetStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-003")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();

        _context.WeatherStations.AddRange(
            inactiveMetStation1, inactiveMetStation2, activeMetStation);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetInactiveMETStationIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("MET-001");
        result.Should().Contain("MET-002");
        result.Should().NotContain("MET-003");
    }

    [Fact]
    public async Task GetInactiveMETStationIdsAsync_WithNoInactiveMETStations_ReturnsEmptyCollection()
    {
        // Arrange
        var activeMetStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();
        await _context.WeatherStations.AddAsync(activeMetStation, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetInactiveMETStationIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(Skip = "ExecuteUpdateAsync is not supported by in-memory database provider")]
    public async Task SetActiveStationsWithDataAsync_WithInactiveStationsHavingData_ActivatesStations()
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
        var result = await _repository.SetAllStationsWithDataToActiveAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1); // Only MET-001 should be activated (MET-003 is already active)

        var updatedStation1 = await _context.WeatherStations
            .FirstOrDefaultAsync(ws => ws.StationId == "MET-001", TestContext.Current.CancellationToken);
        var updatedStation2 = await _context.WeatherStations
            .FirstOrDefaultAsync(ws => ws.StationId == "MET-002", TestContext.Current.CancellationToken);
        var updatedStation3 = await _context.WeatherStations
            .FirstOrDefaultAsync(ws => ws.StationId == "MET-003", TestContext.Current.CancellationToken);

        updatedStation1!.IsActive.Should().BeTrue();
        updatedStation2!.IsActive.Should().BeFalse();
        updatedStation3!.IsActive.Should().BeTrue();
    }

    [Fact(Skip = "ExecuteUpdateAsync is not supported by in-memory database provider")]
    public async Task SetActiveStationsWithDataAsync_WithNoInactiveStationsHavingData_ReturnsZero()
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
        var result = await _repository.SetAllStationsWithDataToActiveAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0);
    }

    [Fact(Skip = "ExecuteUpdateAsync is not supported by in-memory database provider")]
    public async Task SetInactiveStationsWithoutDataAsync_WithActiveStationsWithoutData_DeactivatesStations()
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
        var result = await _repository.SetAllStationsWithoutDataToInactiveAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1); // Only MET-001 should be deactivated

        var updatedStation1 = await _context.WeatherStations
            .FirstOrDefaultAsync(ws => ws.StationId == "MET-001", TestContext.Current.CancellationToken);
        var updatedStation2 = await _context.WeatherStations
            .FirstOrDefaultAsync(ws => ws.StationId == "MET-002", TestContext.Current.CancellationToken);
        var updatedStation3 = await _context.WeatherStations
            .FirstOrDefaultAsync(ws => ws.StationId == "MET-003", TestContext.Current.CancellationToken);

        updatedStation1!.IsActive.Should().BeFalse();
        updatedStation2!.IsActive.Should().BeTrue();
        updatedStation3!.IsActive.Should().BeFalse();
    }

    [Fact(Skip = "ExecuteUpdateAsync is not supported by in-memory database provider")]
    public async Task SetInactiveStationsWithoutDataAsync_WithNoActiveStationsWithoutData_ReturnsZero()
    {
        // Arrange
        var activeStationWithData = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();
        await _context.WeatherStations.AddAsync(activeStationWithData, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var stationData = TestDataBuilders.StationData()
            .WithStationId("MET-001")
            .Build();
        await _context.StationData.AddAsync(stationData, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.SetAllStationsWithoutDataToInactiveAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

