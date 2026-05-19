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
    public async Task GetActiveMETStationIdsAsync_WithActiveMETStations_ReturnsStationIds()
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
        var result = await _repository.GetActiveMETStationIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("MET-001");
        result.Should().Contain("MET-002");
        result.Should().NotContain("MET-003");
        result.Should().NotContain("HOLFUY-001");
    }

    [Fact]
    public async Task GetActiveMETStationIdsAsync_WithNoActiveMETStations_ReturnsEmptyCollection()
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
        var result = await _repository.GetActiveMETStationIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInactiveMETStationIdsAsync_WithInactiveMETStations_ReturnsStationIds()
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
        var result = await _repository.GetInactiveMETStationIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("MET-001");
        result.Should().Contain("MET-002");
        result.Should().NotContain("MET-003");
    }

    [Fact]
    public async Task GetInactiveMETStationIdsAsync_WithNoInactiveMETStations_ReturnsEmptyCollection()
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
        var result = await _repository.GetInactiveMETStationIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStationIdsByProviderAsync_WithMatchingProvider_ReturnsAllProviderStations()
    {
        // Arrange
        var activePortWindStation = TestDataBuilders.WeatherStation()
            .WithStationId("PW-001")
            .WithProvider("PortWind")
            .WithIsActive(true)
            .Build();
        var inactivePortWindStation = TestDataBuilders.WeatherStation()
            .WithStationId("PW-002")
            .WithProvider("PortWind")
            .WithIsActive(false)
            .Build();
        var otherProviderStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();

        _context.WeatherStations.AddRange(activePortWindStation, inactivePortWindStation, otherProviderStation);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetStationIdsByProviderAsync("PortWind", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(["PW-001", "PW-002"]);
    }

    [Fact]
    public async Task GetActiveStationIdsByProviderAsync_WithMatchingProvider_ReturnsOnlyActiveProviderStations()
    {
        // Arrange
        var activePortWindStation = TestDataBuilders.WeatherStation()
            .WithStationId("PW-001")
            .WithProvider("PortWind")
            .WithIsActive(true)
            .Build();
        var inactivePortWindStation = TestDataBuilders.WeatherStation()
            .WithStationId("PW-002")
            .WithProvider("PortWind")
            .WithIsActive(false)
            .Build();
        var activeMetStation = TestDataBuilders.WeatherStation()
            .WithStationId("MET-001")
            .WithProvider("MET")
            .WithIsActive(true)
            .Build();

        _context.WeatherStations.AddRange(activePortWindStation, inactivePortWindStation, activeMetStation);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetActiveStationIdsByProviderAsync("PortWind", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(["PW-001"]);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

