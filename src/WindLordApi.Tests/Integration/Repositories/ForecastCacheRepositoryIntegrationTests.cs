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
/// Integration tests for ForecastCacheRepository methods that use UpsertRange and ExecuteDeleteAsync.
/// These tests use a real PostgreSQL database via Testcontainers to verify the actual
/// production code path, since these operations are not supported by in-memory database.
/// </summary>
[Collection("PostgreSQL Integration Tests")]
public class ForecastCacheRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlTestContainer _container;
    private ApplicationDbContext _context = null!;
    private ForecastCacheRepository _repository = null!;
    private readonly Mock<ILogger<ForecastCacheRepository>> _loggerMock;

    public ForecastCacheRepositoryIntegrationTests(PostgreSqlTestContainer container)
    {
        _container = container;
        _loggerMock = new Mock<ILogger<ForecastCacheRepository>>();
    }

    public async ValueTask InitializeAsync()
    {
        // Ensure database schema is created
        await _container.EnsureDatabaseCreatedAsync();

        // Create a fresh context for each test
        _context = _container.CreateDbContext();
        _repository = new ForecastCacheRepository(_context, _loggerMock.Object);

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
    }

    #region UpsertRangeAsync Tests

    [Fact]
    public async Task UpsertRangeAsync_WithEmptyCollection_ReturnsZero()
    {
        // Arrange
        var emptyList = Enumerable.Empty<ForecastCache>();

        // Act
        var result = await _repository.UpsertRangeAsync(emptyList, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task UpsertRangeAsync_WithNewForecasts_InsertsForecasts()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
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
        var result = await _repository.UpsertRangeAsync(forecasts, TestContext.Current.CancellationToken);

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
    public async Task UpsertRangeAsync_WithExistingForecast_UpdatesForecast()
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
        await _context.ForecastCaches.AddAsync(existingForecast, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Verify initial insert
        var initialCount = await _context.ForecastCaches.CountAsync(TestContext.Current.CancellationToken);
        initialCount.Should().Be(1);

        // Clear change tracker to avoid conflicts with upsert
        _context.ChangeTracker.Clear();

        // Create an updated forecast with same LocationId and Time
        var updatedForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(forecastTime)
            .WithTemperature(18.0m)
            .WithWindSpeed(12.0m)
            .Build();

        // Act
        var result = await _repository.UpsertRangeAsync(new[] { updatedForecast }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);

        // Query fresh from database
        _context.ChangeTracker.Clear();
        var savedForecast = await _context.ForecastCaches
            .FirstOrDefaultAsync(f => f.LocationId == location.Id && f.Time == forecastTime, TestContext.Current.CancellationToken);
        savedForecast.Should().NotBeNull();
        savedForecast!.Temperature.Should().Be(18.0m, "temperature should be updated");
        savedForecast.WindSpeed.Should().Be(12.0m, "wind speed should be updated");

        // Verify no duplicate was created
        var totalCount = await _context.ForecastCaches.CountAsync(TestContext.Current.CancellationToken);
        totalCount.Should().Be(1, "should update existing record, not create duplicate");
    }

    [Fact]
    public async Task UpsertRangeAsync_WithExistingYrForecast_DoesNotOverwriteWithOpenMeteo()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var forecastTime = DateTime.UtcNow.AddHours(1);
        var existingYrForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(forecastTime)
            .WithTemperature(15.5m)
            .WithWindSpeed(10.0m)
            .WithWeatherCode("partlycloudy_day")
            .WithIsYrData(true)
            .Build();
        await _context.ForecastCaches.AddAsync(existingYrForecast, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _context.ChangeTracker.Clear();

        var incomingOpenMeteoForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(forecastTime)
            .WithTemperature(21.0m)
            .WithWindSpeed(13.0m)
            .WithWeatherCode("rain")
            .WithIsYrData(false)
            .Build();

        // Act
        var result = await _repository.UpsertRangeAsync(new[] { incomingOpenMeteoForecast }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0, "a conflicting Open-Meteo row should be ignored when the existing row is Yr-backed");

        _context.ChangeTracker.Clear();
        var savedForecast = await _context.ForecastCaches
            .SingleAsync(f => f.LocationId == location.Id && f.Time == forecastTime, TestContext.Current.CancellationToken);

        savedForecast.IsYrData.Should().BeTrue("existing Yr-backed rows should keep precedence over Open-Meteo conflicts");
        savedForecast.Temperature.Should().Be(15.5m);
        savedForecast.WindSpeed.Should().Be(10.0m);
        savedForecast.WeatherCode.Should().Be("partlycloudy_day");

        var totalCount = await _context.ForecastCaches.CountAsync(TestContext.Current.CancellationToken);
        totalCount.Should().Be(1, "conflicting Open-Meteo upserts should not create duplicates");
    }

    [Fact]
    public async Task UpsertRangeAsync_WithExistingOpenMeteoForecast_UpdatesWithYr()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var forecastTime = DateTime.UtcNow.AddHours(1);
        var existingOpenMeteoForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(forecastTime)
            .WithTemperature(15.5m)
            .WithWindSpeed(10.0m)
            .WithWeatherCode("rain")
            .WithIsYrData(false)
            .Build();
        await _context.ForecastCaches.AddAsync(existingOpenMeteoForecast, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _context.ChangeTracker.Clear();

        var incomingYrForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(forecastTime)
            .WithTemperature(18.0m)
            .WithWindSpeed(12.0m)
            .WithWeatherCode("clearsky_day")
            .WithIsYrData(true)
            .Build();

        // Act
        var result = await _repository.UpsertRangeAsync(new[] { incomingYrForecast }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);

        _context.ChangeTracker.Clear();
        var savedForecast = await _context.ForecastCaches
            .SingleAsync(f => f.LocationId == location.Id && f.Time == forecastTime, TestContext.Current.CancellationToken);

        savedForecast.IsYrData.Should().BeTrue("a later Yr-backed row should replace an existing Open-Meteo row for the same key");
        savedForecast.Temperature.Should().Be(18.0m);
        savedForecast.WindSpeed.Should().Be(12.0m);
        savedForecast.WeatherCode.Should().Be("clearsky_day");

        var totalCount = await _context.ForecastCaches.CountAsync(TestContext.Current.CancellationToken);
        totalCount.Should().Be(1, "conflicting Yr upserts should update the existing row instead of inserting a duplicate");
    }

    [Fact]
    public async Task UpsertRangeAsync_WithMixedInsertAndUpdate_HandlesCorrectly()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var existingTime = DateTime.UtcNow.AddHours(1);
        var existingForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(existingTime)
            .WithTemperature(15.5m)
            .Build();
        await _context.ForecastCaches.AddAsync(existingForecast, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Create mix of update and insert
        var updatedForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(existingTime)
            .WithTemperature(20.0m)
            .Build();
        var newForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddHours(2))
            .WithTemperature(16.0m)
            .Build();

        // Act
        var result = await _repository.UpsertRangeAsync(new[] { updatedForecast, newForecast }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);

        // Detach tracked entities to force fresh query
        _context.ChangeTracker.Clear();

        var savedForecasts = await _context.ForecastCaches
            .Where(f => f.LocationId == location.Id)
            .OrderBy(f => f.Time)
            .ToListAsync(TestContext.Current.CancellationToken);
        savedForecasts.Should().HaveCount(2);
        savedForecasts[0].Temperature.Should().Be(20.0m, "existing forecast should be updated");
        savedForecasts[1].Temperature.Should().Be(16.0m, "new forecast should be inserted");
    }

    [Fact]
    public async Task UpsertRangeAsync_WithDifferentLocations_InsertsAll()
    {
        // Arrange
        var location1 = TestDataBuilders.ParaglidingLocation()
            .WithName("Location 1")
            .WithFlightlogId("FL-001")
            .Build();
        var location2 = TestDataBuilders.ParaglidingLocation()
            .WithName("Location 2")
            .WithFlightlogId("FL-002")
            .Build();
        await _context.ParaglidingLocations.AddRangeAsync(new[] { location1, location2 }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var forecastTime = DateTime.UtcNow.AddHours(1);
        var forecast1 = TestDataBuilders.ForecastCache()
            .WithLocationId(location1.Id)
            .WithTime(forecastTime)
            .WithTemperature(15.5m)
            .Build();
        var forecast2 = TestDataBuilders.ForecastCache()
            .WithLocationId(location2.Id)
            .WithTime(forecastTime)
            .WithTemperature(20.0m)
            .Build();

        // Act
        var result = await _repository.UpsertRangeAsync(new[] { forecast1, forecast2 }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);
        var location1Forecasts = await _context.ForecastCaches
            .Where(f => f.LocationId == location1.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        location1Forecasts.Should().HaveCount(1);
        location1Forecasts[0].Temperature.Should().Be(15.5m);

        var location2Forecasts = await _context.ForecastCaches
            .Where(f => f.LocationId == location2.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        location2Forecasts.Should().HaveCount(1);
        location2Forecasts[0].Temperature.Should().Be(20.0m);
    }

    [Fact]
    public async Task UpsertRangeAsync_WithAllForecastFields_UpdatesAllFields()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var forecastTime = DateTime.UtcNow.AddHours(1);
        var existingForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(forecastTime)
            .WithTemperature(10.0m)
            .WithWindSpeed(5.0m)
            .WithWindDirection(180)
            .Build();
        await _context.ForecastCaches.AddAsync(existingForecast, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Clear change tracker to avoid tracking conflicts with upsert
        _context.ChangeTracker.Clear();

        // Create updated forecast with many fields changed
        var updatedForecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(forecastTime)
            .WithTemperature(20.0m)
            .WithWindSpeed(15.0m)
            .WithWindDirection(270)
            .Build();

        // Act
        var result = await _repository.UpsertRangeAsync(new[] { updatedForecast }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);

        // Query fresh from database
        _context.ChangeTracker.Clear();
        var savedForecast = await _context.ForecastCaches
            .FirstOrDefaultAsync(f => f.LocationId == location.Id && f.Time == forecastTime, TestContext.Current.CancellationToken);
        savedForecast.Should().NotBeNull();
        savedForecast!.Temperature.Should().Be(20.0m);
        savedForecast.WindSpeed.Should().Be(15.0m);
        savedForecast.WindDirection.Should().Be(270);
    }

    [Fact]
    public async Task UpsertRangeAsync_WithCompositeKeyConflict_UpdatesCorrectly()
    {
        // Test that the composite key (LocationId, Time) works correctly
        // Same time, different location should insert (not conflict)
        // Same location, same time should update (conflict)

        // Arrange
        var location1 = TestDataBuilders.ParaglidingLocation()
            .WithName("Location 1")
            .WithFlightlogId("FL-COMP-001")
            .Build();
        var location2 = TestDataBuilders.ParaglidingLocation()
            .WithName("Location 2")
            .WithFlightlogId("FL-COMP-002")
            .Build();
        await _context.ParaglidingLocations.AddRangeAsync(new[] { location1, location2 }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var forecastTime = DateTime.UtcNow.AddHours(1);

        // Insert initial forecasts for both locations at same time
        var forecast1 = TestDataBuilders.ForecastCache()
            .WithLocationId(location1.Id)
            .WithTime(forecastTime)
            .WithTemperature(15.0m)
            .Build();
        var forecast2 = TestDataBuilders.ForecastCache()
            .WithLocationId(location2.Id)
            .WithTime(forecastTime)
            .WithTemperature(20.0m)
            .Build();
        await _context.ForecastCaches.AddRangeAsync(new[] { forecast1, forecast2 }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Clear change tracker to avoid tracking conflicts with upsert
        _context.ChangeTracker.Clear();

        // Now upsert with same location1 + time (should update) and location2 + different time (should insert)
        var updatedForecast1 = TestDataBuilders.ForecastCache()
            .WithLocationId(location1.Id)
            .WithTime(forecastTime) // Same time - should update
            .WithTemperature(25.0m)
            .Build();
        var newForecast2 = TestDataBuilders.ForecastCache()
            .WithLocationId(location2.Id)
            .WithTime(forecastTime.AddHours(1)) // Different time - should insert
            .WithTemperature(22.0m)
            .Build();

        // Act
        var result = await _repository.UpsertRangeAsync(new[] { updatedForecast1, newForecast2 }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);

        // Query fresh from database
        _context.ChangeTracker.Clear();
        var allForecasts = await _context.ForecastCaches.OrderBy(f => f.LocationId).ThenBy(f => f.Time).ToListAsync(TestContext.Current.CancellationToken);
        allForecasts.Should().HaveCount(3, "should have: location1 (updated), location2 (original), location2 (new)");

        // Location 1 should be updated
        var location1Forecasts = allForecasts.Where(f => f.LocationId == location1.Id).ToList();
        location1Forecasts.Should().HaveCount(1);
        location1Forecasts[0].Temperature.Should().Be(25.0m, "should be updated");

        // Location 2 should have both forecasts
        var location2Forecasts = allForecasts.Where(f => f.LocationId == location2.Id).OrderBy(f => f.Time).ToList();
        location2Forecasts.Should().HaveCount(2);
        location2Forecasts[0].Temperature.Should().Be(20.0m, "original should remain");
        location2Forecasts[1].Temperature.Should().Be(22.0m, "new should be inserted");
    }

    #endregion

    #region DeleteOldForecastsAsync Tests

    [Fact]
    public async Task DeleteOldForecastsAsync_WithOldForecasts_DeletesThemCorrectly()
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

        await _context.ForecastCaches.AddRangeAsync(new[] { oldForecast1, oldForecast2 }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _repository.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);
        var remainingForecasts = await _context.ForecastCaches.ToListAsync(TestContext.Current.CancellationToken);
        remainingForecasts.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteOldForecastsAsync_WithNewForecasts_KeepsThemIntact()
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

        await _context.ForecastCaches.AddRangeAsync(new[] { newForecast1, newForecast2 }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _repository.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0);
        var remainingForecasts = await _context.ForecastCaches.ToListAsync(TestContext.Current.CancellationToken);
        remainingForecasts.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteOldForecastsAsync_WithMixedOldAndNewForecasts_DeletesOnlyOld()
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
            new[] { oldForecast1, oldForecast2, newForecast1, newForecast2 },
            TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _repository.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);
        var remainingForecasts = await _context.ForecastCaches
            .OrderBy(f => f.Temperature)
            .ToListAsync(TestContext.Current.CancellationToken);
        remainingForecasts.Should().HaveCount(2);
        remainingForecasts.Should().Contain(f => f.Temperature == 15.0m);
        remainingForecasts.Should().Contain(f => f.Temperature == 16.0m);
        remainingForecasts.Should().NotContain(f => f.Temperature == 10.0m);
        remainingForecasts.Should().NotContain(f => f.Temperature == 11.0m);
    }

    [Fact]
    public async Task DeleteOldForecastsAsync_WithNoForecasts_ReturnsZero()
    {
        // Arrange
        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _repository.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task DeleteOldForecastsAsync_WithExactCutoffTime_DoesNotDeleteForecastAtCutoff()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation().Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        var beforeCutoff = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(cutoffTime.AddSeconds(-1))
            .WithTemperature(10.0m)
            .Build();
        var atCutoff = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(cutoffTime)
            .WithTemperature(15.0m)
            .Build();
        var afterCutoff = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(cutoffTime.AddSeconds(1))
            .WithTemperature(20.0m)
            .Build();

        await _context.ForecastCaches.AddRangeAsync(new[] { beforeCutoff, atCutoff, afterCutoff }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1, "only the forecast before cutoff should be deleted");
        var remainingForecasts = await _context.ForecastCaches
            .OrderBy(f => f.Temperature)
            .ToListAsync(TestContext.Current.CancellationToken);
        remainingForecasts.Should().HaveCount(2);
        remainingForecasts.Should().Contain(f => f.Temperature == 15.0m, "forecast at cutoff time should remain");
        remainingForecasts.Should().Contain(f => f.Temperature == 20.0m, "forecast after cutoff should remain");
        remainingForecasts.Should().NotContain(f => f.Temperature == 10.0m, "forecast before cutoff should be deleted");
    }

    [Fact]
    public async Task DeleteOldForecastsAsync_WithMultipleLocations_DeletesOldFromAllLocations()
    {
        // Arrange
        var location1 = TestDataBuilders.ParaglidingLocation()
            .WithName("Location 1")
            .WithFlightlogId("FL-DEL-001")
            .Build();
        var location2 = TestDataBuilders.ParaglidingLocation()
            .WithName("Location 2")
            .WithFlightlogId("FL-DEL-002")
            .Build();
        await _context.ParaglidingLocations.AddRangeAsync(new[] { location1, location2 }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var oldForecastLoc1 = TestDataBuilders.ForecastCache()
            .WithLocationId(location1.Id)
            .WithTime(DateTime.UtcNow.AddDays(-10))
            .WithTemperature(10.0m)
            .Build();
        var oldForecastLoc2 = TestDataBuilders.ForecastCache()
            .WithLocationId(location2.Id)
            .WithTime(DateTime.UtcNow.AddDays(-9))
            .WithTemperature(11.0m)
            .Build();
        var newForecastLoc1 = TestDataBuilders.ForecastCache()
            .WithLocationId(location1.Id)
            .WithTime(DateTime.UtcNow.AddHours(1))
            .WithTemperature(15.0m)
            .Build();
        var newForecastLoc2 = TestDataBuilders.ForecastCache()
            .WithLocationId(location2.Id)
            .WithTime(DateTime.UtcNow.AddHours(2))
            .WithTemperature(16.0m)
            .Build();

        await _context.ForecastCaches.AddRangeAsync(
            new[] { oldForecastLoc1, oldForecastLoc2, newForecastLoc1, newForecastLoc2 },
            TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _repository.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2, "should delete old forecasts from both locations");
        var remainingForecasts = await _context.ForecastCaches.ToListAsync(TestContext.Current.CancellationToken);
        remainingForecasts.Should().HaveCount(2);
        remainingForecasts.Should().Contain(f => f.LocationId == location1.Id && f.Temperature == 15.0m);
        remainingForecasts.Should().Contain(f => f.LocationId == location2.Id && f.Temperature == 16.0m);
    }

    [Fact]
    public async Task DeleteOldForecastsAsync_LogsDebugMessage()
    {
        // Arrange
        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        await _repository.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert - Verify logging was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleted") && v.ToString()!.Contains("forecasts")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "should log debug message after deletion");
    }

    #endregion
}

