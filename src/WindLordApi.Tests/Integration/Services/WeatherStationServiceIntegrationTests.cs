using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Data;
using WindLordApi.Data.Models;
using WindLordApi.Data.Repositories;
using WindLordApi.Data.Services;
using WindLordApi.Tests.Helpers;
using Xunit;

namespace WindLordApi.Tests.Integration.Services;

/// <summary>
/// Integration tests for WeatherStationService that require a real database.
/// Tests UpsertManyAsync which uses transactions and batch processing.
/// Note: Tests for SetAllStationsWithDataToActiveAsync and SetAllStationsWithoutDataToInactiveAsync
/// are in WeatherStationRepositoryIntegrationTests since these are simple pass-through methods.
/// </summary>
[Collection("PostgreSQL Integration Tests")]
public class WeatherStationServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlTestContainer _container;
    private ApplicationDbContext _context = null!;
    private IUnitOfWork _unitOfWork = null!;
    private WeatherStationService _service = null!;
    private readonly Mock<ILogger<WeatherStationService>> _serviceLoggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;

    public WeatherStationServiceIntegrationTests(PostgreSqlTestContainer container)
    {
        _container = container;
        _serviceLoggerMock = new Mock<ILogger<WeatherStationService>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
    }

    public async ValueTask InitializeAsync()
    {
        // Ensure database schema is created
        await _container.EnsureDatabaseCreatedAsync();

        // Create a fresh context for each test
        _context = _container.CreateDbContext();

        // Setup logger factory to return mock loggers
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_serviceLoggerMock.Object);

        _unitOfWork = new UnitOfWork(_context, _loggerFactoryMock.Object);
        _service = new WeatherStationService(_unitOfWork, _serviceLoggerMock.Object);

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

        _unitOfWork?.Dispose();
    }

    #region UpsertManyAsync Tests

    [Fact]
    public async Task UpsertManyAsync_WithValidStations_InsertsNewStations()
    {
        // Arrange
        var station1 = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithName("Station 1")
            .WithProvider("MET")
            .Build();
        var station2 = TestDataBuilders.WeatherStation()
            .WithStationId("MET-002")
            .WithName("Station 2")
            .WithProvider("MET")
            .Build();
        var stations = new[] { station1, station2 };

        // Act
        var result = await _service.UpsertManyAsync(stations, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);
        var savedStations = await _context.WeatherStations.ToListAsync(TestContext.Current.CancellationToken);
        savedStations.Should().HaveCount(2);
        savedStations.Should().Contain(s => s.StationId == "MET-001");
        savedStations.Should().Contain(s => s.StationId == "MET-002");
    }

    [Fact]
    public async Task UpsertManyAsync_WithExistingStations_UpdatesStations()
    {
        // Arrange
        var existingStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithName("Original Name")
            .WithProvider("MET")
            .WithCoordinates(60.0m, 10.0m)
            .Build();
        await _context.WeatherStations.AddAsync(existingStation, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Detach the existing entity to avoid tracking conflicts
        _context.Entry(existingStation).State = EntityState.Detached;

        var updatedStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithName("Updated Name")
            .WithProvider("MET")
            .WithCoordinates(61.0m, 11.0m)
            .Build();
        var stations = new[] { updatedStation };

        // Act
        var result = await _service.UpsertManyAsync(stations, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);

        // Query fresh from database to verify update
        var savedStation = await _context.WeatherStations
            .AsNoTracking()
            .FirstOrDefaultAsync(ws => ws.StationId == "MET-001", TestContext.Current.CancellationToken);
        savedStation.Should().NotBeNull();
        savedStation!.Name.Should().Be("Updated Name");
        savedStation.Latitude.Should().Be(61.0m);
        savedStation.Longitude.Should().Be(11.0m);
    }

    [Fact]
    public async Task UpsertManyAsync_WithLargeBatch_ProcessesInBatches()
    {
        // Arrange - Create 1500 stations (more than batch size of 1000)
        var stations = new List<WeatherStation>();
        for (int i = 1; i <= 1500; i++)
        {
            stations.Add(TestDataBuilders.WeatherStation()
                .WithStationId($"MET-{i:D4}")
                .WithName($"Station {i}")
                .WithProvider("MET")
                .Build());
        }

        // Act
        var result = await _service.UpsertManyAsync(stations.ToArray(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1500);
        var count = await _context.WeatherStations.CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1500);

        // Verify first and last stations to ensure all were inserted
        var firstStation = await _context.WeatherStations
            .FirstOrDefaultAsync(ws => ws.StationId == "MET-0001", TestContext.Current.CancellationToken);
        var lastStation = await _context.WeatherStations
            .FirstOrDefaultAsync(ws => ws.StationId == "MET-1500", TestContext.Current.CancellationToken);

        firstStation.Should().NotBeNull();
        lastStation.Should().NotBeNull();
    }

    [Fact]
    public async Task UpsertManyAsync_WithMixedNullAndValidElements_FiltersNulls()
    {
        // Arrange
        var validStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithName("Valid Station")
            .WithProvider("MET")
            .Build();
        var stations = new WeatherStation[] { null!, validStation, null! };

        // Act
        var result = await _service.UpsertManyAsync(stations, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
        var savedStations = await _context.WeatherStations.ToListAsync(TestContext.Current.CancellationToken);
        savedStations.Should().HaveCount(1);
        savedStations.First().StationId.Should().Be("MET-001");
    }

    [Fact]
    public async Task UpsertManyAsync_WithMixOfNewAndExisting_HandlesCorrectly()
    {
        // Arrange - Add an existing station
        var existingStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithName("Existing Station")
            .WithProvider("MET")
            .Build();
        await _context.WeatherStations.AddAsync(existingStation, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Detach the existing entity to avoid tracking conflicts
        _context.Entry(existingStation).State = EntityState.Detached;

        // Prepare mix of new and existing
        var updatedExisting = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithName("Updated Existing")
            .WithProvider("MET")
            .Build();
        var newStation1 = TestDataBuilders.WeatherStation()
            .WithStationId("MET-002")
            .WithName("New Station 1")
            .WithProvider("MET")
            .Build();
        var newStation2 = TestDataBuilders.WeatherStation()
            .WithStationId("MET-003")
            .WithName("New Station 2")
            .WithProvider("MET")
            .Build();
        var stations = new[] { updatedExisting, newStation1, newStation2 };

        // Act
        var result = await _service.UpsertManyAsync(stations, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(3);

        // Query fresh from database to verify
        var savedStations = await _context.WeatherStations
            .AsNoTracking()
            .OrderBy(ws => ws.StationId)
            .ToListAsync(TestContext.Current.CancellationToken);
        savedStations.Should().HaveCount(3);
        savedStations[0].StationId.Should().Be("MET-001");
        savedStations[0].Name.Should().Be("Updated Existing");
        savedStations[1].StationId.Should().Be("MET-002");
        savedStations[2].StationId.Should().Be("MET-003");
    }

    [Fact]
    public async Task UpsertManyAsync_PreservesIsActiveFlag_WhenUpdatingExisting()
    {
        // Arrange - Add an existing active station
        var existingStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithName("Original")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();
        await _context.WeatherStations.AddAsync(existingStation, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Detach the existing entity to avoid tracking conflicts
        _context.Entry(existingStation).State = EntityState.Detached;

        // Update the station (with IsActive=false in the update data)
        var updatedStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithName("Updated")
            .WithProvider("MET")
            .WithIsActive(false) // This should be ignored by upsert
            .Build();
        var stations = new[] { updatedStation };

        // Act
        var result = await _service.UpsertManyAsync(stations, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);

        // Query fresh from database to verify
        var savedStation = await _context.WeatherStations
            .AsNoTracking()
            .FirstOrDefaultAsync(ws => ws.StationId == "MET-001", TestContext.Current.CancellationToken);
        savedStation.Should().NotBeNull();
        savedStation!.Name.Should().Be("Updated");
        // IsActive should remain true (not updated by upsert)
        savedStation.IsActive.Should().BeTrue();
    }

    #endregion
}

