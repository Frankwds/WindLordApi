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
        // Fetch data from MET Frost API, map it, and upsert to database
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var metFrostClient = scope.ServiceProvider.GetRequiredService<IMetFrostClient>();
            var stationDataService = scope.ServiceProvider.GetRequiredService<IStationDataService>();

            // Fetch data from MET Frost API
            var response = await metFrostClient.FetchMetStationDataAsync(["SN97350", "SN246400"], stoppingToken);
            _logger.LogInformation("Fetched {Count} data points from MET Frost API", response.Data.Count);

            // Map MET observations to StationData
            var stationDataList = MetFrostMapping.MapMetObservationsToStationData(response.Data);
            _logger.LogInformation("Mapped {Count} valid station data records from {DataPointCount} data points",
                stationDataList.Count, response.Data.Count);

            // Upsert the mapped data to database
            if (stationDataList.Count > 0)
            {
                await stationDataService.UpsertManyAsync(stationDataList.ToArray(), stoppingToken);
                _logger.LogInformation("Successfully upserted {Count} station data records", stationDataList.Count);
            }
            else
            {
                _logger.LogWarning("No valid station data records to upsert");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MET Frost data");
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
