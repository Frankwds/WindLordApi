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

namespace WindLordApi.Tests.Unit.Services;

/// <summary>
/// Unit tests for ForecastCacheService input validation logic.
/// Database-dependent operations (UpsertMany, DeleteOldForecasts) are tested in integration tests.
/// </summary>
public class ForecastCacheServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<ForecastCacheService>> _loggerMock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ForecastCacheService _service;

    public ForecastCacheServiceTests()
    {
        _context = InMemoryDbContextFactory.Create();
        var loggerFactory = new Mock<ILoggerFactory>();
        _loggerMock = new Mock<ILogger<ForecastCacheService>>();
        // Setup CreateLogger to return our mock logger
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);
        _unitOfWork = new UnitOfWork(_context, loggerFactory.Object);
        _service = new ForecastCacheService(_unitOfWork, _loggerMock.Object);
    }

    #region Input Validation Tests

    [Fact]
    public async Task UpsertManyAsync_WithNullArray_ThrowsArgumentException()
    {
        // Arrange
        ForecastCache[]? forecasts = null;

        // Act
        var act = async () => await _service.UpsertManyAsync(forecasts!, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Forecasts array cannot be null or empty*");
    }

    [Fact]
    public async Task UpsertManyAsync_WithEmptyArray_ThrowsArgumentException()
    {
        // Arrange
        var forecasts = Array.Empty<ForecastCache>();

        // Act
        var act = async () => await _service.UpsertManyAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Forecasts array cannot be null or empty*");
    }

    [Fact]
    public async Task UpsertManyAsync_WithOnlyNullElements_ThrowsArgumentException()
    {
        // Arrange
        var forecasts = new ForecastCache[] { null!, null! };

        // Act
        var act = async () => await _service.UpsertManyAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Forecasts array cannot contain only null elements*");
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        _unitOfWork.Dispose();
    }
}

