using WindLordApi.Data.Services;
using WindLordApi.Integrations.MetFrost;

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


        // Test database connection
        // try
        // {
        //     using var scope = _serviceProvider.CreateScope();
        //     var stationDataService = scope.ServiceProvider.GetRequiredService<IStationDataService>();
        //     // Get all data for a station
        //     var allData = await stationDataService.GetByStationIdAsync("1576", stoppingToken);

        //     _logger.LogInformation("Found {Count} records for station {StationId}", allData.Count(), "1576");

        //     // Modify the first record's wind speed to 10 for testing upsert logic
        //     var dataArray = allData.ToArray();
        //     if (dataArray.Length > 0)
        //     {
        //         dataArray[0].WindSpeed = 123;
        //         // Round down to nearest 15-minute interval (00, 15, 30, or 45)
        //         var now = DateTime.UtcNow;
        //         var roundedMinutes = now.Minute - (now.Minute % 15);
        //         dataArray[0].UpdatedAt = new DateTime(now.Year, now.Month, now.Day, now.Hour, roundedMinutes, 0, DateTimeKind.Utc);
        //         _logger.LogInformation("Modified first record's wind speed to 10 for testing");
        //     }

        //     // Upsert the data - reuse the same scope and service
        //     await stationDataService.UpsertManyAsync(dataArray, stoppingToken);
        // }
        // catch (Exception ex)
        // {
        //     _logger.LogError(ex, "Error connecting to database");
        // }
        // Fetch data from MET Frost API
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var metFrostClient = scope.ServiceProvider.GetRequiredService<IMetFrostClient>();
            var data = await metFrostClient.FetchMetStationDataAsync(["SN97350", "SN246400"], stoppingToken);
            _logger.LogInformation("Fetched {Data}", data.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data from MET Frost API");
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
