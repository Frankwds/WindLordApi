using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WindLordApi.Data;
using WindLordApi.Data.Models;
using WindLordApi.Data.Repositories;
using WindLordApi.Tests.Helpers;
using Xunit;

namespace WindLordApi.Tests.Unit.Repositories;

/// <summary>
/// Tests for the generic Repository<T> base class.
/// </summary>
public class RepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Repository<WeatherStation> _repository;

    public RepositoryTests()
    {
        _context = InMemoryDbContextFactory.Create();
        _repository = new Repository<WeatherStation>(_context);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsEntity()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("TEST-001")
            .Build();
        await _context.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByIdAsync(station.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.StationId.Should().Be("TEST-001");
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithNoData_ReturnsEmptyCollection()
    {
        // Act
        var result = await _repository.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithData_ReturnsAllEntities()
    {
        // Arrange
        var station1 = TestDataBuilders.WeatherStation().WithStationId("TEST-001").Build();
        var station2 = TestDataBuilders.WeatherStation().WithStationId("TEST-002").Build();
        _context.WeatherStations.AddRange(station1, station2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.StationId == "TEST-001");
        result.Should().Contain(s => s.StationId == "TEST-002");
    }

    [Fact]
    public async Task FindAsync_WithMatchingPredicate_ReturnsMatchingEntities()
    {
        // Arrange
        var activeStation = TestDataBuilders.WeatherStation()
            .WithStationId("ACTIVE-001")
            .WithIsActive(true)
            .Build();
        var inactiveStation = TestDataBuilders.WeatherStation()
            .WithStationId("INACTIVE-001")
            .WithIsActive(false)
            .Build();
        _context.WeatherStations.AddRange(activeStation, inactiveStation);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.FindAsync(ws => ws.IsActive == true, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(s => s.StationId == "ACTIVE-001");
        result.Should().NotContain(s => s.StationId == "INACTIVE-001");
    }

    [Fact]
    public async Task FindAsync_WithNoMatches_ReturnsEmptyCollection()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("TEST-001")
            .WithProvider("MET")
            .Build();
        await _context.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.FindAsync(ws => ws.Provider == "HOLFUY", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithMatchingEntity_ReturnsFirstMatch()
    {
        // Arrange
        var station1 = TestDataBuilders.WeatherStation().WithStationId("TEST-001").Build();
        var station2 = TestDataBuilders.WeatherStation().WithStationId("TEST-002").Build();
        _context.WeatherStations.AddRange(station1, station2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.FirstOrDefaultAsync(ws => ws.StationId == "TEST-001", TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.StationId.Should().Be("TEST-001");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithNoMatch_ReturnsNull()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation().WithStationId("TEST-001").Build();
        await _context.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.FirstOrDefaultAsync(ws => ws.StationId == "NONEXISTENT", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AnyAsync_WithMatchingEntity_ReturnsTrue()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("TEST-001")
            .WithIsActive(true)
            .Build();
        await _context.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.AnyAsync(ws => ws.IsActive == true, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_WithNoMatch_ReturnsFalse()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("TEST-001")
            .WithIsActive(false)
            .Build();
        await _context.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.AnyAsync(ws => ws.IsActive == true, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_WithValidEntity_AddsEntityToContext()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation().WithStationId("TEST-001").Build();

        // Act
        var result = await _repository.AddAsync(station, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(station);
        var savedStation = await _context.WeatherStations.FindAsync(new object[] { station.Id }, TestContext.Current.CancellationToken);
        savedStation.Should().NotBeNull();
        savedStation!.StationId.Should().Be("TEST-001");
    }

    [Fact]
    public async Task AddRangeAsync_WithMultipleEntities_AddsAllEntities()
    {
        // Arrange
        var station1 = TestDataBuilders.WeatherStation().WithStationId("TEST-001").Build();
        var station2 = TestDataBuilders.WeatherStation().WithStationId("TEST-002").Build();
        var stations = new[] { station1, station2 };

        // Act
        await _repository.AddRangeAsync(stations, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var count = await _context.WeatherStations.CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(2);
    }

    [Fact]
    public void Update_WithModifiedEntity_MarksEntityAsModified()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("TEST-001")
            .WithName("Original Name")
            .Build();
        _context.WeatherStations.Add(station);
        _context.SaveChanges();

        // Act
        station.Name = "Updated Name";
        _repository.Update(station);
        _context.SaveChanges();

        // Assert
        var updatedStation = _context.WeatherStations.Find(station.Id);
        updatedStation!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public void UpdateRange_WithMultipleEntities_MarksAllAsModified()
    {
        // Arrange
        var station1 = TestDataBuilders.WeatherStation().WithStationId("TEST-001").Build();
        var station2 = TestDataBuilders.WeatherStation().WithStationId("TEST-002").Build();
        _context.WeatherStations.AddRange(station1, station2);
        _context.SaveChanges();

        // Act
        station1.Name = "Updated 1";
        station2.Name = "Updated 2";
        _repository.UpdateRange(new[] { station1, station2 });
        _context.SaveChanges();

        // Assert
        var updated1 = _context.WeatherStations.Find(station1.Id);
        var updated2 = _context.WeatherStations.Find(station2.Id);
        updated1!.Name.Should().Be("Updated 1");
        updated2!.Name.Should().Be("Updated 2");
    }

    [Fact]
    public void Remove_WithExistingEntity_MarksEntityForDeletion()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation().WithStationId("TEST-001").Build();
        _context.WeatherStations.Add(station);
        _context.SaveChanges();

        // Act
        _repository.Remove(station);
        _context.SaveChanges();

        // Assert
        var deletedStation = _context.WeatherStations.Find(station.Id);
        deletedStation.Should().BeNull();
    }

    [Fact]
    public void RemoveRange_WithMultipleEntities_MarksAllForDeletion()
    {
        // Arrange
        var station1 = TestDataBuilders.WeatherStation().WithStationId("TEST-001").Build();
        var station2 = TestDataBuilders.WeatherStation().WithStationId("TEST-002").Build();
        _context.WeatherStations.AddRange(station1, station2);
        _context.SaveChanges();

        // Act
        _repository.RemoveRange(new[] { station1, station2 });
        _context.SaveChanges();

        // Assert
        var count = _context.WeatherStations.Count();
        count.Should().Be(0);
    }

    [Fact]
    public void Query_ReturnsQueryable()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation().WithStationId("TEST-001").Build();
        _context.WeatherStations.Add(station);
        _context.SaveChanges();

        // Act
        var queryable = _repository.Query();
        var result = queryable.Where(ws => ws.StationId == "TEST-001").ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().StationId.Should().Be("TEST-001");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

