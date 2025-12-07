using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WindLordApi.Data;
using WindLordApi.Data.Repositories;
using WindLordApi.Tests.Helpers;

namespace WindLordApi.Tests.Unit.Repositories;

/// <summary>
/// Tests for UnitOfWork transaction management and repository access.
/// </summary>
public class UnitOfWorkTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        _context = InMemoryDbContextFactory.Create();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        // Setup CreateLogger to return a mock logger for any type
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        _unitOfWork = new UnitOfWork(_context, _loggerFactoryMock.Object);
    }

    [Fact]
    public void WeatherStations_Property_ReturnsRepository()
    {
        // Act
        var repository = _unitOfWork.WeatherStations;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IWeatherStationRepository>();
    }

    [Fact]
    public void WeatherStations_Property_ReturnsSameInstanceOnMultipleCalls()
    {
        // Act
        var repository1 = _unitOfWork.WeatherStations;
        var repository2 = _unitOfWork.WeatherStations;

        // Assert
        repository1.Should().BeSameAs(repository2);
    }

    [Fact]
    public void StationData_Property_ReturnsRepository()
    {
        // Act
        var repository = _unitOfWork.StationData;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IStationDataRepository>();
    }

    [Fact]
    public void LatestStationData_Property_ReturnsRepository()
    {
        // Act
        var repository = _unitOfWork.LatestStationData;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<ILatestStationDataRepository>();
    }

    [Fact]
    public async Task SaveChangesAsync_WithChanges_SavesChanges()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("TEST-001")
            .Build();
        await _unitOfWork.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);

        // Act
        var result = await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeGreaterThan(0);
        var savedStation = await _context.WeatherStations.FindAsync(new object[] { station.Id }, TestContext.Current.CancellationToken);
        savedStation.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ReturnsZero()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0);
    }

    [Fact(Skip = "Requires relational database provider - transactions not supported by in-memory database")]
    public async Task BeginTransactionAsync_CreatesTransaction()
    {
        // Act
        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Dispose();
    }

    [Fact(Skip = "Requires relational database provider - transactions not supported by in-memory database")]
    public async Task CommitTransactionAsync_CommitsTransaction()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("TEST-001")
            .Build();
        await _unitOfWork.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);

        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Act
        await _unitOfWork.CommitTransactionAsync(transaction, TestContext.Current.CancellationToken);

        // Assert
        var savedStation = await _context.WeatherStations.FindAsync([station.Id], TestContext.Current.CancellationToken);
        savedStation.Should().NotBeNull();
    }

    [Fact(Skip = "Requires relational database provider - transactions not supported by in-memory database")]
    public async Task RollbackTransactionAsync_RollsBackTransaction()
    {
        // Arrange
        var station = TestDataBuilders.WeatherStation()
            .WithStationId("TEST-001")
            .Build();
        await _unitOfWork.WeatherStations.AddAsync(station, TestContext.Current.CancellationToken);

        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Act
        await _unitOfWork.RollbackTransactionAsync(transaction, TestContext.Current.CancellationToken);

        // Assert
        // Note: In-memory database doesn't support true rollback, but we verify the method doesn't throw
        transaction.Should().NotBeNull();
    }

    [Fact]
    public async Task DisposeAsync_DisposesContext()
    {
        // Arrange
        var context = InMemoryDbContextFactory.Create();
        var unitOfWork = new UnitOfWork(context, _loggerFactoryMock.Object);

        // Act
        await unitOfWork.DisposeAsync();

        // Assert
        // Verify context is disposed (accessing it should throw)
        var act = () => context.WeatherStations.Count();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_DisposesContext()
    {
        // Arrange
        var context = InMemoryDbContextFactory.Create();
        var unitOfWork = new UnitOfWork(context, _loggerFactoryMock.Object);

        // Act
        unitOfWork.Dispose();

        // Assert
        // Verify context is disposed
        var act = () => context.WeatherStations.Count();
        act.Should().Throw<ObjectDisposedException>();
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _context.Dispose();
    }
}

