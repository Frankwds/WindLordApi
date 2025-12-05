using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace WindLordApi.Data.Repositories;

/// <summary>
/// Unit of Work implementation for managing transactions and repositories.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ILoggerFactory _loggerFactory;
    private IWeatherStationRepository? _weatherStations;
    private IStationDataRepository? _stationData;
    private ILatestStationDataRepository? _latestStationData;

    public UnitOfWork(ApplicationDbContext context, ILoggerFactory loggerFactory)
    {
        _context = context;
        _loggerFactory = loggerFactory;
    }

    public IWeatherStationRepository WeatherStations
    {
        get
        {
            _weatherStations ??= new WeatherStationRepository(_context, _loggerFactory.CreateLogger<WeatherStationRepository>());
            return _weatherStations;
        }
    }

    public IStationDataRepository StationData
    {
        get
        {
            _stationData ??= new StationDataRepository(_context);
            return _stationData;
        }
    }

    public ILatestStationDataRepository LatestStationData
    {
        get
        {
            _latestStationData ??= new LatestStationDataRepository(_context);
            return _latestStationData;
        }
    }

    public ApplicationDbContext Context => _context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        await transaction.RollbackAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
    public void Dispose()
    {
        _context.Dispose();
    }
}

