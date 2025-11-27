using WindLordApi.Data.Services;

namespace WindLordApi.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {



        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }


            // Test database connection
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var stationDataService = scope.ServiceProvider.GetRequiredService<IStationDataService>();
                // Get all data for a station
                var allData = await stationDataService.GetByStationIdAsync("1576", stoppingToken);

                _logger.LogInformation("Found {Count} records for station {StationId}", allData.Count(), "STATION_001");


                var stationDataService2 = scope.ServiceProvider.GetRequiredService<IStationDataService>();
                var allData2 = await stationDataService2.GetByStationIdAsync("1576", stoppingToken);
                _logger.LogInformation("Found {Count} records for station {StationId}", allData2.Count(), "STATION_002");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to database");
            }
            await Task.Delay(10000, stoppingToken);


        }
    }
}
