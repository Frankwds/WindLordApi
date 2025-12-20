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

namespace WindLordApi.Tests.Unit.Services;

/// <summary>
/// Tests for WeatherStationService validation and business logic.
/// </summary>
public class WeatherStationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<WeatherStationService>> _loggerMock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly WeatherStationService _service;

    public WeatherStationServiceTests()
    {
        _context = InMemoryDbContextFactory.Create();
        var loggerFactory = new Mock<ILoggerFactory>();
        _loggerMock = new Mock<ILogger<WeatherStationService>>();
        // Setup CreateLogger to return our mock logger
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);
        _unitOfWork = new UnitOfWork(_context, loggerFactory.Object);
        _service = new WeatherStationService(_unitOfWork, _loggerMock.Object);
    }

    [Fact]
    public async Task GetActiveMETStationIdsAsync_DelegatesToRepository()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();
        await _context.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _service.GetActiveMETStationIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Contain("MET-001");
    }

    [Fact]
    public async Task GetInactiveMETStationIdsAsync_DelegatesToRepository()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(false)
            .Build();
        await _context.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _service.GetInactiveMETStationIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Contain("MET-001");
    }

    [Fact]
    public async Task UpsertManyAsync_WithNullArray_ThrowsArgumentException()
    {
        // Arrange
        WeatherStation[]? stations = null;

        // Act
        var act = async () => await _service.UpsertManyAsync(stations!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Weather stations array cannot be null or empty*");
    }

    [Fact]
    public async Task UpsertManyAsync_WithEmptyArray_ThrowsArgumentException()
    {
        // Arrange
        var stations = Array.Empty<WeatherStation>();

        // Act
        var act = async () => await _service.UpsertManyAsync(stations, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Weather stations array cannot be null or empty*");
    }

    [Fact]
    public async Task UpsertManyAsync_WithOnlyNullElements_ThrowsArgumentException()
    {
        // Arrange
        var stations = new WeatherStation[] { null!, null! };

        // Act
        var act = async () => await _service.UpsertManyAsync(stations, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Weather stations array cannot contain only null elements*");
    }

    [Fact(Skip = "Requires relational database provider - transactions not supported by in-memory database")]
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

    [Fact(Skip = "Requires relational database provider - transactions not supported by in-memory database")]
    public async Task UpsertManyAsync_WithExistingStations_UpdatesStations()
    {
        // Arrange
        var existingStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithName("Original Name")
            .WithProvider("MET")
            .Build();
        await _context.WeatherStations.AddAsync(existingStation, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var updatedStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithName("Updated Name")
            .WithProvider("MET")
            .Build();
        var stations = new[] { updatedStation };

        // Act
        var result = await _service.UpsertManyAsync(stations, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
        var savedStation = await _context.WeatherStations
            .FirstOrDefaultAsync(ws => ws.StationId == "MET-001", TestContext.Current.CancellationToken);
        savedStation!.Name.Should().Be("Updated Name");
    }

    [Fact(Skip = "Requires relational database provider - transactions not supported by in-memory database")]
    public async Task UpsertManyAsync_WithLargeBatch_ProcessesInBatches()
    {
        // Arrange - Create 1500 stations (more than batch size of 1000)
        var stations = new List<WeatherStation>();
        for (int i = 1; i <= 1500; i++)
        {
            stations.Add(TestDataBuilders.WeatherStation()
                .WithStationId($"MET-{i:D4}")
                .WithProvider("MET")
                .Build());
        }

        // Act
        var result = await _service.UpsertManyAsync(stations.ToArray(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1500);
        var count = await _context.WeatherStations.CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1500);
    }

    [Fact(Skip = "Requires relational database provider - transactions not supported by in-memory database")]
    public async Task UpsertManyAsync_WithMixedNullAndValidElements_FiltersNulls()
    {
        // Arrange
        var validStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
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

    [Fact(Skip = "Requires relational database provider - raw SQL not supported by in-memory database")]
    public async Task SetAllStationsWithDataToActiveAsync_DelegatesToRepository()
    {
        // Arrange
        var inactiveStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(false)
            .Build();
        await _context.WeatherStations.AddAsync(inactiveStation, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var stationData = TestDataBuilders.StationData()
            .WithStationId("MET-001")
            .Build();
        await _context.StationData.AddAsync(stationData, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _service.SetAllStationsWithDataToActiveAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
    }

    [Fact(Skip = "Requires relational database provider - raw SQL not supported by in-memory database")]
    public async Task SetAllStationsWithoutDataToInactiveAsync_DelegatesToRepository()
    {
        // Arrange
        var activeStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();
        await _context.WeatherStations.AddAsync(activeStation, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _service.SetAllStationsWithoutDataToInactiveAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
    }

    public void Dispose()
    {
        _context.Dispose();
        _unitOfWork.Dispose();
    }
}

