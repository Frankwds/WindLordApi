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
/// Unit tests for ForecastCacheRepository methods that can work with in-memory database.
/// PostgreSQL-specific operations (UpsertRange, ExecuteDeleteAsync) are tested in integration tests.
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

