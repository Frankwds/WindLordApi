using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WindLordApi.Data;
using WindLordApi.Data.Models;
using WindLordApi.Data.Repositories;
using WindLordApi.Tests.Helpers;
using Xunit;

namespace WindLordApi.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for StationDataRepository.DeleteOlderThanAsync.
/// </summary>
[Collection("PostgreSQL Integration Tests")]
public class StationDataRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlTestContainer _container;
    private ApplicationDbContext _context = null!;
    private StationDataRepository _repository = null!;

    public StationDataRepositoryIntegrationTests(PostgreSqlTestContainer container)
    {
        _container = container;
    }

    public async ValueTask InitializeAsync()
    {
        await _container.EnsureDatabaseCreatedAsync();

        _context = _container.CreateDbContext();
        _repository = new StationDataRepository(_context);

        await _context.StationData.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.WeatherStations.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null)
        {
            await _context.StationData.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.WeatherStations.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.DisposeAsync();
        }
    }

    [Fact]
    public async Task DeleteOlderThanAsync_WithNoData_ReturnsZero()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-24);

        var result = await _repository.DeleteOlderThanAsync(cutoffTime, TestContext.Current.CancellationToken);

        result.Should().Be(0);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_WithMixedOldAndNewData_DeletesOnlyOld()
    {
        var station = await SeedWeatherStationAsync("RET-001");

        var oldData = TestDataBuilders.StationData()
            .WithStationId(station.StationId)
            .WithUpdatedAt(DateTime.UtcNow.AddHours(-30))
            .WithWindSpeed(5.0m)
            .Build();
        var newData = TestDataBuilders.StationData()
            .WithStationId(station.StationId)
            .WithUpdatedAt(DateTime.UtcNow.AddHours(-1))
            .WithWindSpeed(10.0m)
            .Build();

        await _context.StationData.AddRangeAsync(new[] { oldData, newData }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cutoffTime = DateTime.UtcNow.AddHours(-24);

        var result = await _repository.DeleteOlderThanAsync(cutoffTime, TestContext.Current.CancellationToken);

        result.Should().Be(1);
        var remaining = await _context.StationData.ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().ContainSingle(sd => sd.WindSpeed == 10.0m);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_WithExactCutoffTime_DoesNotDeleteRowAtCutoff()
    {
        var station = await SeedWeatherStationAsync("RET-002");
        var cutoffTime = DateTime.UtcNow.AddHours(-24);

        var beforeCutoff = TestDataBuilders.StationData()
            .WithStationId(station.StationId)
            .WithUpdatedAt(cutoffTime.AddSeconds(-1))
            .WithWindSpeed(5.0m)
            .Build();
        var atCutoff = TestDataBuilders.StationData()
            .WithStationId(station.StationId)
            .WithUpdatedAt(cutoffTime)
            .WithWindSpeed(10.0m)
            .Build();

        await _context.StationData.AddRangeAsync(new[] { beforeCutoff, atCutoff }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.DeleteOlderThanAsync(cutoffTime, TestContext.Current.CancellationToken);

        result.Should().Be(1);
        var remaining = await _context.StationData.ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().ContainSingle(sd => sd.WindSpeed == 10.0m);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_WithMultipleStations_DeletesOldFromAllStations()
    {
        var station1 = await SeedWeatherStationAsync("RET-003");
        var station2 = await SeedWeatherStationAsync("RET-004");

        var oldStation1 = TestDataBuilders.StationData()
            .WithStationId(station1.StationId)
            .WithUpdatedAt(DateTime.UtcNow.AddHours(-30))
            .WithWindSpeed(5.0m)
            .Build();
        var oldStation2 = TestDataBuilders.StationData()
            .WithStationId(station2.StationId)
            .WithUpdatedAt(DateTime.UtcNow.AddHours(-30))
            .WithWindSpeed(6.0m)
            .Build();
        var newStation1 = TestDataBuilders.StationData()
            .WithStationId(station1.StationId)
            .WithUpdatedAt(DateTime.UtcNow.AddHours(-1))
            .WithWindSpeed(15.0m)
            .Build();

        await _context.StationData.AddRangeAsync(
            new[] { oldStation1, oldStation2, newStation1 },
            TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cutoffTime = DateTime.UtcNow.AddHours(-24);

        var result = await _repository.DeleteOlderThanAsync(cutoffTime, TestContext.Current.CancellationToken);

        result.Should().Be(2);
        var remaining = await _context.StationData.ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().ContainSingle(sd => sd.WindSpeed == 15.0m);
    }

    private async Task<WeatherStation> SeedWeatherStationAsync(string stationId)
    {
        var station = TestDataBuilders.WeatherStation()
            .WithStationId(stationId)
            .Build();

        await _context.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return station;
    }
}
