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
    public async Task GetActiveStationIdsByProviderAsync_WithActiveProviderStations_ReturnsStationIds()
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
        var result = await _repository.GetActiveStationIdsByProviderAsync("MET", TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("MET-001");
        result.Should().Contain("MET-002");
        result.Should().NotContain("MET-003");
        result.Should().NotContain("HOLFUY-001");
    }

    [Fact]
    public async Task GetActiveStationIdsByProviderAsync_WithNoActiveProviderStations_ReturnsEmptyCollection()
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
        var result = await _repository.GetActiveStationIdsByProviderAsync("MET", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInactiveStationIdsByProviderAsync_WithInactiveProviderStations_ReturnsStationIds()
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
        var result = await _repository.GetInactiveStationIdsByProviderAsync("MET", TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("MET-001");
        result.Should().Contain("MET-002");
        result.Should().NotContain("MET-003");
    }

    [Fact]
    public async Task GetInactiveStationIdsByProviderAsync_WithNoInactiveProviderStations_ReturnsEmptyCollection()
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
        var result = await _repository.GetInactiveStationIdsByProviderAsync("MET", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

