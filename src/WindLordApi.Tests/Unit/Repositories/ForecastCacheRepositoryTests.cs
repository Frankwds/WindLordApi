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
/// Tests for ForecastCacheRepository custom query methods.
/// </summary>
public class ForecastCacheRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<ForecastCacheRepository>> _loggerMock;
    private readonly ForecastCacheRepository _repository;

    public ForecastCacheRepositoryTests()
    {
        _context = InMemoryDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<ForecastCacheRepository>>();
        _repository = new ForecastCacheRepository(_context, _loggerMock.Object);
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

    [Fact(Skip = "UpsertRange (FlexLabs) is not supported by in-memory database provider")]
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
            .Build();
        var forecast2 = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTime(DateTime.UtcNow.AddHours(2))
            .WithTemperature(16.0m)
            .Build();

        var forecasts = new[] { forecast1, forecast2 };

        // Act
        var result = await _repository.UpsertRangeAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);
        var savedForecasts = await _context.ForecastCaches.ToListAsync(TestContext.Current.CancellationToken);
        savedForecasts.Should().HaveCount(2);
        savedForecasts.Should().Contain(f => f.Temperature == 15.5m);
        savedForecasts.Should().Contain(f => f.Temperature == 16.0m);
    }

    [Fact(Skip = "UpsertRange (FlexLabs) is not supported by in-memory database provider")]
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
        var savedForecast = await _context.ForecastCaches
            .FirstOrDefaultAsync(f => f.LocationId == location.Id && f.Time == forecastTime, TestContext.Current.CancellationToken);
        savedForecast.Should().NotBeNull();
        savedForecast!.Temperature.Should().Be(18.0m);
        savedForecast.WindSpeed.Should().Be(12.0m);
    }

    [Fact(Skip = "UpsertRange (FlexLabs) is not supported by in-memory database provider")]
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
        var savedForecasts = await _context.ForecastCaches
            .Where(f => f.LocationId == location.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        savedForecasts.Should().HaveCount(2);
        savedForecasts.Should().Contain(f => f.Temperature == 20.0m); // Updated
        savedForecasts.Should().Contain(f => f.Temperature == 16.0m); // New
    }

    #endregion

    #region DeleteOldForecastsAsync Tests

    [Fact(Skip = "ExecuteDeleteAsync is not supported by in-memory database provider")]
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

    [Fact(Skip = "ExecuteDeleteAsync is not supported by in-memory database provider")]
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

    [Fact(Skip = "ExecuteDeleteAsync is not supported by in-memory database provider")]
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
        var remainingForecasts = await _context.ForecastCaches.ToListAsync(TestContext.Current.CancellationToken);
        remainingForecasts.Should().HaveCount(2);
        remainingForecasts.Should().Contain(f => f.Temperature == 15.0m);
        remainingForecasts.Should().Contain(f => f.Temperature == 16.0m);
        remainingForecasts.Should().NotContain(f => f.Temperature == 10.0m);
        remainingForecasts.Should().NotContain(f => f.Temperature == 11.0m);
    }

    [Fact(Skip = "ExecuteDeleteAsync is not supported by in-memory database provider")]
    public async Task DeleteOldForecastsAsync_WithNoForecasts_ReturnsZero()
    {
        // Arrange
        var cutoffTime = DateTime.UtcNow.AddDays(-7);

        // Act
        var result = await _repository.DeleteOldForecastsAsync(cutoffTime, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Foreign Key Relationship Tests

    [Fact]
    public async Task ForecastCache_WithValidParaglidingLocation_LinksProperly()
    {
        // Arrange
        var location = TestDataBuilders.ParaglidingLocation()
            .WithName("Test Location")
            .Build();
        await _context.ParaglidingLocations.AddAsync(location, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var forecast = TestDataBuilders.ForecastCache()
            .WithLocationId(location.Id)
            .WithTemperature(15.5m)
            .Build();
        await _context.ForecastCaches.AddAsync(forecast, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var savedForecast = await _context.ForecastCaches
            .Include(f => f.ParaglidingLocation)
            .FirstOrDefaultAsync(f => f.Id == forecast.Id, TestContext.Current.CancellationToken);

        // Assert
        savedForecast.Should().NotBeNull();
        savedForecast!.ParaglidingLocation.Should().NotBeNull();
        savedForecast.ParaglidingLocation!.Name.Should().Be("Test Location");
    }

    [Fact]
    public async Task QueryForecastsByLocationId_WithMultipleForecasts_ReturnsCorrectForecasts()
    {
        // Arrange
        var location1 = TestDataBuilders.ParaglidingLocation().WithName("Location 1").Build();
        var location2 = TestDataBuilders.ParaglidingLocation().WithName("Location 2").Build();
        await _context.ParaglidingLocations.AddRangeAsync(new[] { location1, location2 }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var forecast1 = TestDataBuilders.ForecastCache()
            .WithLocationId(location1.Id)
            .WithTemperature(15.5m)
            .Build();
        var forecast2 = TestDataBuilders.ForecastCache()
            .WithLocationId(location1.Id)
            .WithTemperature(16.0m)
            .Build();
        var forecast3 = TestDataBuilders.ForecastCache()
            .WithLocationId(location2.Id)
            .WithTemperature(20.0m)
            .Build();

        await _context.ForecastCaches.AddRangeAsync(new[] { forecast1, forecast2, forecast3 }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var location1Forecasts = await _context.ForecastCaches
            .Where(f => f.LocationId == location1.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        location1Forecasts.Should().HaveCount(2);
        location1Forecasts.Should().Contain(f => f.Temperature == 15.5m);
        location1Forecasts.Should().Contain(f => f.Temperature == 16.0m);
        location1Forecasts.Should().NotContain(f => f.Temperature == 20.0m);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}

