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
/// Integration tests for ForecastCacheService using PostgreSQL database.
/// These tests verify upsert operations, ExecuteDeleteAsync, and transaction handling
/// that cannot be tested with in-memory database provider.
/// </summary>
[Collection("PostgreSQL Integration Tests")]
public class ForecastCacheServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlTestContainer _container;
    private ApplicationDbContext _context = null!;
    private IUnitOfWork _unitOfWork = null!;
    private ForecastCacheService _service = null!;
    private readonly Mock<ILogger<ForecastCacheService>> _loggerMock;

    public ForecastCacheServiceIntegrationTests(PostgreSqlTestContainer container)
    {
        _container = container;
        _loggerMock = new Mock<ILogger<ForecastCacheService>>();
    }

    public async ValueTask InitializeAsync()
    {
        // Ensure database schema is created
        await _container.EnsureDatabaseCreatedAsync();

        // Create a fresh context for each test
        _context = _container.CreateDbContext();

        // Create logger factory for UnitOfWork
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _unitOfWork = new UnitOfWork(_context, loggerFactory.Object);
        _service = new ForecastCacheService(_unitOfWork, _loggerMock.Object);

        // Clean up any existing data
        _context.ForecastCaches.RemoveRange(_context.ForecastCaches);
        _context.ParaglidingLocations.RemoveRange(_context.ParaglidingLocations);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up test data after each test
        if (_context != null)
        {
            // Use raw SQL to delete to avoid foreign key issues
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM forecast_cache",
                TestContext.Current.CancellationToken);
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM all_paragliding_locations",
                TestContext.Current.CancellationToken);

            await _context.DisposeAsync();
        }

        if (_unitOfWork != null)
        {
            _unitOfWork.Dispose();
        }
    }

    #region UpsertManyAsync Tests

    [Fact]
    public async Task UpsertManyAsync_WithValidForecasts_InsertsNewForecasts()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation()
            .WithName("Test Location")
            .Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var forecast1 = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddHours(1))
            .WithTemperature(15.5m)
            .WithWindSpeed(10.0m)
            .Build();
        var forecast2 = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddHours(2))
            .WithTemperature(16.0m)
            .WithWindSpeed(12.0m)
            .Build();
        var forecasts = new[] { forecast1, forecast2 };

        // Act
        var result = await _service.UpsertManyAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);
        var savedForecasts = await _context.ForecastCaches
            .Where(f => f.LocationId == location.Id)
            .OrderBy(f => f.Time)
            .ToListAsync(TestContext.Current.CancellationToken);
        savedForecasts.Should().HaveCount(2);
        savedForecasts[0].Temperature.Should().Be(15.5m);
        savedForecasts[0].WindSpeed.Should().Be(10.0m);
        savedForecasts[1].Temperature.Should().Be(16.0m);
        savedForecasts[1].WindSpeed.Should().Be(12.0m);
    }

    [Fact]
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
            .WithWindSpeed(10.0m)
            .Build();

        // Insert initial forecast
        await _service.UpsertManyAsync(new[] { existingForecast }, TestContext.Current.CancellationToken);

        // Verify initial insert
        var initialCount = await _context.ForecastCaches.CountAsync(TestContext.Current.CancellationToken);
        initialCount.Should().Be(1);

        // Create an updated forecast with same LocationId and Time
        var updatedForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(forecastTime)
            .WithTemperature(20.0m)
            .WithWindSpeed(15.0m)
            .Build();

        // Act
        var result = await _service.UpsertManyAsync(new[] { updatedForecast }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
        var savedForecasts = await _context.ForecastCaches
            .Where(f => f.LocationId == location.Id && f.Time == forecastTime)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Should be updated, not duplicated
        savedForecasts.Should().HaveCount(1, "the forecast should be updated, not inserted as a duplicate");
        savedForecasts.First().Temperature.Should().Be(20.0m, "temperature should be updated");
        savedForecasts.First().WindSpeed.Should().Be(15.0m, "wind speed should be updated");
    }

    [Fact]
    public async Task UpsertManyAsync_WithLargeBatch_ProcessesInBatches()
    {
        // Arrange - Create 1500 forecasts (more than batch size of 1000)
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var forecasts = Enumerable.Range(0, 1500)
            .Select(i => TestDataBuilders.ForecastCache()
                .WithLocationId(location.Id)
                .WithTime(DateTime.UtcNow.AddHours(i + 1))
                .WithTemperature(15.0m + i * 0.01m)
                .Build())
            .ToArray();

        // Act
        var result = await _service.UpsertManyAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1500, "all forecasts should be processed");
        var count = await _context.ForecastCaches
            .Where(f => f.LocationId == location.Id)
            .CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1500, "all forecasts should be saved to database");

        // Verify some sample records to ensure data integrity
        var allForecasts = await _context.ForecastCaches
            .OrderBy(f => f.Time)
            .ToListAsync(TestContext.Current.CancellationToken);

        var firstForecast = allForecasts.First();
        firstForecast.Temperature.Should().Be(15.0m);

        var lastForecast = allForecasts.Last();
        // Last forecast: index 1499, so 15.0 + 1499 * 0.01 = 29.99, but with floating point it might be 30.0
        lastForecast.Temperature.Should().BeInRange(29.99m, 30.0m);
    }

    [Fact]
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
        result.Should().Be(1, "only the valid forecast should be processed");
        var savedForecasts = await _context.ForecastCaches.ToListAsync(TestContext.Current.CancellationToken);
        savedForecasts.Should().HaveCount(1, "only the valid forecast should be saved");
        savedForecasts.First().Temperature.Should().Be(15.5m);
    }

    #endregion

    #region DeleteOldForecastsAsync Tests

    [Fact]
    public async Task DeleteOldForecastsAsync_WithOldForecasts_DeletesCorrectly()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var oldForecast1 = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddDays(-10))
            .WithTemperature(10.0m)
            .Build();
        var oldForecast2 = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddDays(-8))
            .WithTemperature(11.0m)
            .Build();
        var newForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddHours(1))
            .WithTemperature(15.0m)
            .Build();

        await _context.ForecastCaches.AddRangeAsync(
            new[] { oldForecast1, oldForecast2, newForecast },
            TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _service.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2, "two old forecasts should be deleted");
        var remainingForecasts = await _context.ForecastCaches.ToListAsync(TestContext.Current.CancellationToken);
        remainingForecasts.Should().HaveCount(1, "only the new forecast should remain");
        remainingForecasts.First().Temperature.Should().Be(15.0m, "the remaining forecast should be the new one");
    }

    [Fact]
    public async Task DeleteOldForecastsAsync_WithNoOldForecasts_ReturnsZero()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var newForecast1 = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddHours(1))
            .WithTemperature(15.0m)
            .Build();
        var newForecast2 = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddHours(2))
            .WithTemperature(16.0m)
            .Build();

        await _context.ForecastCaches.AddRangeAsync(
            new[] { newForecast1, newForecast2 },
            TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _service.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0, "no old forecasts should be deleted");
        var remainingForecasts = await _context.ForecastCaches.ToListAsync(TestContext.Current.CancellationToken);
        remainingForecasts.Should().HaveCount(2, "all forecasts should remain");
    }

    [Fact]
    public async Task DeleteOldForecastsAsync_LogsDebugMessages()
    {
        // Arrange
        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        await _service.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert - Verify logging calls were made
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleting forecast data older than")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "should log debug message before deletion");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Forecast data cleanup completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "should log debug message after deletion");
    }

    #endregion
}

