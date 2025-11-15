using WindLordApi.Data;
using Microsoft.EntityFrameworkCore;
using WindLordApi.Data.Services;

namespace WindLordApi.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        // Test database connection
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var stationDataService = scope.ServiceProvider.GetRequiredService<IStationDataService>();
            // Get all data for a station
            var allData = await stationDataService.GetByStationIdAsync("1576", stoppingToken);

            _logger.LogInformation("Found {Count} records for station {StationId}", allData.Count(), "STATION_001");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to database");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(10000, stoppingToken);
        }
    }
}
