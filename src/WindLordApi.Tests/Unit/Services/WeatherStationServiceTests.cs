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

    public void Dispose()
    {
        _context.Dispose();
        _unitOfWork.Dispose();
    }
}

