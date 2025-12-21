using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Data;
using WindLordApi.Data.Repositories;
using WindLordApi.Tests.Helpers;
using Xunit;

namespace WindLordApi.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for UnitOfWork transaction management.
/// These tests use a real PostgreSQL database via Testcontainers to verify
/// transaction behavior, since transactions are not supported by in-memory database.
/// </summary>
[Collection("PostgreSQL Integration Tests")]
public class UnitOfWorkIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlTestContainer _container;
    private ApplicationDbContext _context = null!;
    private UnitOfWork _unitOfWork = null!;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;

    public UnitOfWorkIntegrationTests(PostgreSqlTestContainer container)
    {
        _container = container;
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        // Setup CreateLogger to return a mock logger for any type
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
    }

    public async ValueTask InitializeAsync()
    {
        // Ensure database schema is created
        await _container.EnsureDatabaseCreatedAsync();

        // Create a fresh context for each test
        _context = _container.CreateDbContext();
        _unitOfWork = new UnitOfWork(_context, _loggerFactoryMock.Object);

        // Clean up any existing data
        _context.WeatherStations.RemoveRange(_context.WeatherStations);
        _context.StationData.RemoveRange(_context.StationData);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        // Dispose unit of work first (which disposes the context)
        if (_unitOfWork != null)
        {
            await _unitOfWork.DisposeAsync();
        }
        
        // Clean up test data using a fresh context
        await using var cleanupContext = _container.CreateDbContext();
        var stations = await cleanupContext.WeatherStations.ToListAsync(TestContext.Current.CancellationToken);
        cleanupContext.WeatherStations.RemoveRange(stations);
        
        var stationData = await cleanupContext.StationData.ToListAsync(TestContext.Current.CancellationToken);
        cleanupContext.StationData.RemoveRange(stationData);
        
        await cleanupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BeginTransactionAsync_CreatesTransaction()
    {
        // Act
        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Should().BeAssignableTo<IDbContextTransaction>();
        
        // Clean up
        await transaction.DisposeAsync();
    }

    [Fact]
    public async Task CommitTransactionAsync_CommitsTransaction()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("TEST-001")
            .Build();
        await _unitOfWork.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Modify the station within the transaction
        station.Name = "Updated Station Name";
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _unitOfWork.CommitTransactionAsync(transaction, TestContext.Current.CancellationToken);

        // Assert
        // Create a new context to verify the changes were committed
        await using var verificationContext = _container.CreateDbContext();
        var savedStation = await verificationContext.WeatherStations.FindAsync(new object[] { station.Id }, TestContext.Current.CancellationToken);
        savedStation.Should().NotBeNull();
        savedStation!.Name.Should().Be("Updated Station Name");
        
        // Clean up
        await transaction.DisposeAsync();
    }

    [Fact]
    public async Task RollbackTransactionAsync_RollsBackTransaction()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("TEST-001")
            .WithName("Original Station Name")
            .Build();
        await _unitOfWork.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        var originalName = station.Name;
        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Modify the station within the transaction
        station.Name = "Updated Station Name";
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _unitOfWork.RollbackTransactionAsync(transaction, TestContext.Current.CancellationToken);

        // Assert
        // Create a new context to verify the changes were rolled back
        await using var verificationContext = _container.CreateDbContext();
        var savedStation = await verificationContext.WeatherStations.FindAsync(new object[] { station.Id }, TestContext.Current.CancellationToken);
        savedStation.Should().NotBeNull();
        savedStation!.Name.Should().Be(originalName);
        savedStation.Name.Should().NotBe("Updated Station Name");
        
        // Clean up
        await transaction.DisposeAsync();
    }

    [Fact]
    public async Task Transaction_WithMultipleRepositories_CommitsAllChanges()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("TEST-001")
            .Build();
        await _unitOfWork.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Add data using multiple repositories within the same transaction
        var stationData = TestDataBuilders.StationData()
            .WithStationId("TEST-001")
            .Build();
        await _unitOfWork.StationData.AddAsync(stationData, TestContext.Current.CancellationToken);
        
        station.Name = "Updated in Transaction";
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _unitOfWork.CommitTransactionAsync(transaction, TestContext.Current.CancellationToken);

        // Assert
        // Create a new context to verify all changes were committed
        await using var verificationContext = _container.CreateDbContext();
        var savedStation = await verificationContext.WeatherStations.FindAsync(new object[] { station.Id }, TestContext.Current.CancellationToken);
        savedStation.Should().NotBeNull();
        savedStation!.Name.Should().Be("Updated in Transaction");

        var savedStationData = verificationContext.StationData.Where(sd => sd.StationId == "TEST-001").ToList();
        savedStationData.Should().HaveCount(1);
        
        // Clean up
        await transaction.DisposeAsync();
    }

    [Fact]
    public async Task Transaction_WithMultipleRepositories_RollsBackAllChanges()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("TEST-001")
            .WithName("Original Name")
            .Build();
        await _unitOfWork.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Add data using multiple repositories within the same transaction
        var stationData = TestDataBuilders.StationData()
            .WithStationId("TEST-001")
            .Build();
        await _unitOfWork.StationData.AddAsync(stationData, TestContext.Current.CancellationToken);
        
        station.Name = "Updated in Transaction";
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _unitOfWork.RollbackTransactionAsync(transaction, TestContext.Current.CancellationToken);

        // Assert
        // Create a new context to verify all changes were rolled back
        await using var verificationContext = _container.CreateDbContext();
        var savedStation = await verificationContext.WeatherStations.FindAsync(new object[] { station.Id }, TestContext.Current.CancellationToken);
        savedStation.Should().NotBeNull();
        savedStation!.Name.Should().Be("Original Name");
        savedStation.Name.Should().NotBe("Updated in Transaction");

        var savedStationData = verificationContext.StationData.Where(sd => sd.StationId == "TEST-001").ToList();
        savedStationData.Should().BeEmpty();
        
        // Clean up
        await transaction.DisposeAsync();
    }
}

