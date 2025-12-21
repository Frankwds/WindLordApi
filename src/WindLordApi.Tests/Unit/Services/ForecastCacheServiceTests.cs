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
/// Tests for ForecastCacheService validation and business logic.
/// </summary>
public class ForecastCacheServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<ForecastCacheService>> _loggerMock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ForecastCacheService _service;

    public ForecastCacheServiceTests()
    {
        _context = InMemoryDbContextFactory.Create();
        var loggerFactory = new Mock<ILoggerFactory>();
        _loggerMock = new Mock<ILogger<ForecastCacheService>>();
        // Setup CreateLogger to return our mock logger
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);
        _unitOfWork = new UnitOfWork(_context, loggerFactory.Object);
        _service = new ForecastCacheService(_unitOfWork, _loggerMock.Object);
    }

    #region Input Validation Tests

    [Fact]
    public async Task UpsertManyAsync_WithNullArray_ThrowsArgumentException()
    {
        // Arrange
        ForecastCache[]? forecasts = null;

        // Act
        var act = async () => await _service.UpsertManyAsync(forecasts!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Forecasts array cannot be null or empty*");
    }

    [Fact]
    public async Task UpsertManyAsync_WithEmptyArray_ThrowsArgumentException()
    {
        // Arrange
        var forecasts = Array.Empty<ForecastCache>();

        // Act
        var act = async () => await _service.UpsertManyAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Forecasts array cannot be null or empty*");
    }

    [Fact]
    public async Task UpsertManyAsync_WithOnlyNullElements_ThrowsArgumentException()
    {
        // Arrange
        var forecasts = new ForecastCache[] { null!, null! };

        // Act
        var act = async () => await _service.UpsertManyAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Forecasts array cannot contain only null elements*");
    }

    [Fact(Skip = "Requires relational database provider - transactions not supported by in-memory database")]
    public async Task UpsertManyAsync_WithMixedNullAndValidElements_FiltersNulls()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var validForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTemperature(15.5m)
            .Build();
        var forecasts = new ForecastCache[] { null!, validForecast, null! };

        // Act
        var result = await _service.UpsertManyAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
        var savedForecasts = await _context.ForecastCaches.ToListAsync(TestContext.Current.CancellationToken);
        savedForecasts.Should().HaveCount(1);
        savedForecasts.First().Temperature.Should().Be(15.5m);
    }

    #endregion

    #region UpsertManyAsync Tests

    [Fact(Skip = "Requires relational database provider - transactions not supported by in-memory database")]
    public async Task UpsertManyAsync_WithValidForecasts_InsertsNewForecasts()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var forecast1 = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddHours(1))
            .WithTemperature(15.5m)
            .Build();
        var forecast2 = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddHours(2))
            .WithTemperature(16.0m)
            .Build();
        var forecasts = new[] { forecast1, forecast2 };

        // Act
        var result = await _service.UpsertManyAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);
        var savedForecasts = await _context.ForecastCaches.ToListAsync(TestContext.Current.CancellationToken);
        savedForecasts.Should().HaveCount(2);
        savedForecasts.Should().Contain(f => f.Temperature == 15.5m);
        savedForecasts.Should().Contain(f => f.Temperature == 16.0m);
    }

    [Fact(Skip = "Requires relational database provider - transactions not supported by in-memory database")]
    public async Task UpsertManyAsync_WithExistingForecasts_UpdatesForecasts()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var forecastTime = DateTime.UtcNow.AddHours(1);
        var existingForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(forecastTime)
            .WithTemperature(15.5m)
            .Build();
        await _context.ForecastCaches.AddAsync(existingForecast, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var updatedForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(forecastTime)
            .WithTemperature(20.0m)
            .Build();
        var forecasts = new[] { updatedForecast };

        // Act
        var result = await _service.UpsertManyAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
        var savedForecast = await _context.ForecastCaches
            .FirstOrDefaultAsync(f => f.LocationId == location.Id && f.Time == forecastTime, TestContext.Current.CancellationToken);
        savedForecast!.Temperature.Should().Be(20.0m);
    }

    [Fact(Skip = "Requires relational database provider - transactions not supported by in-memory database")]
    public async Task UpsertManyAsync_WithLargeBatch_ProcessesInBatches()
    {
        // Arrange - Create 1500 forecasts (more than batch size of 1000)
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var forecasts = new List<ForecastCache>();
        for (int i = 1; i <= 1500; i++)
        {
            forecasts.Add(TestDataBuilders.ForecastCache()
                .WithLocationId(location.Id)
                .WithTime(DateTime.UtcNow.AddHours(i))
                .WithTemperature(15.0m + i * 0.1m)
                .Build());
        }

        // Act
        var result = await _service.UpsertManyAsync(forecasts.ToArray(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1500);
        var count = await _context.ForecastCaches.CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1500);
    }

    #endregion

    #region DeleteOldForecastsAsync Tests

    [Fact(Skip = "ExecuteDeleteAsync is not supported by in-memory database provider")]
    public async Task DeleteOldForecastsAsync_DelegatesToRepository()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var oldForecast1 = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddDays(-10))
            .Build();
        var oldForecast2 = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddDays(-8))
            .Build();

        await _context.ForecastCaches.AddRangeAsync(new[] { oldForecast1, oldForecast2 }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _service.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);
    }

    [Fact(Skip = "ExecuteDeleteAsync is not supported by in-memory database provider")]
    public async Task DeleteOldForecastsAsync_WithNoOldForecasts_ReturnsZero()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var newForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddHours(1))
            .Build();

        await _context.ForecastCaches.AddAsync(newForecast, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _service.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0);
        var remainingForecasts = await _context.ForecastCaches.ToListAsync(TestContext.Current.CancellationToken);
        remainingForecasts.Should().HaveCount(1);
    }

    [Fact(Skip = "ExecuteDeleteAsync is not supported by in-memory database provider")]
    public async Task DeleteOldForecastsAsync_LogsDebugMessages()
    {
        // Arrange
        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        await _service.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleting forecast data older than")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Forecast data cleanup completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        _unitOfWork.Dispose();
    }
}

