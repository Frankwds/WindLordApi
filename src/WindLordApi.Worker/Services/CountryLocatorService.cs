using Microsoft.Extensions.Logging;
using WindLordApi.Data.Services;
using WindLordApi.Integrations.GoogleGeocoding;

namespace WindLordApi.Worker.Services;

/// <summary>
/// Service for locating countries of weather stations using Google Geocoding API reverse geocoding.
/// Queries stations with missing country (null or "UKJENT"), reverse geocodes their coordinates,
/// and updates Country and IsMain accordingly.
/// </summary>
public class CountryLocatorService : ICountryLocatorService
{
    private readonly IGoogleGeocodingClient _geocodingClient;
    private readonly IWeatherStationService _weatherStationService;
    private readonly ILogger<CountryLocatorService> _logger;

    /// <summary>
    /// Number of stations to process per batch.
    /// Google rate limit is 50 req/s; 40 per batch with 1s delay keeps us safely under.
    /// </summary>
    private const int BatchSize = 40;

    /// <summary>
    /// Delay between batches to respect Google Geocoding API rate limits (50 req/s).
    /// </summary>
    private static readonly TimeSpan DelayBetweenBatches = TimeSpan.FromSeconds(1);

    public CountryLocatorService(
        IGoogleGeocodingClient geocodingClient,
        IWeatherStationService weatherStationService,
        ILogger<CountryLocatorService> logger)
    {
        _geocodingClient = geocodingClient;
        _weatherStationService = weatherStationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> LocateCountriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Fetch all stations with missing country
            _logger.LogInformation("CountryLocator: Fetching weather stations with missing country...");
            var stations = await _weatherStationService.GetStationsWithMissingCountryAsync(cancellationToken);

            if (stations.Count == 0)
            {
                _logger.LogInformation("CountryLocator: No stations with missing country found. Nothing to do.");
                return 0;
            }

            _logger.LogInformation("CountryLocator: Found {Count} stations with missing country. Processing in batches of {BatchSize}...",
                stations.Count, BatchSize);

            var totalLocated = 0;

            // 2. Process in batches
            for (int i = 0; i < stations.Count; i += BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = stations.Skip(i).Take(BatchSize).ToList();
                var batchNumber = (i / BatchSize) + 1;
                var totalBatches = (int)Math.Ceiling((double)stations.Count / BatchSize);

                _logger.LogInformation("CountryLocator: Processing batch {BatchNumber}/{TotalBatches} ({BatchCount} stations)...",
                    batchNumber, totalBatches, batch.Count);

                var batchLocated = 0;

                // 3. Reverse geocode each station in the batch
                foreach (var station in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var country = await _geocodingClient.ReverseGeocodeCountryAsync(
                        station.Latitude, station.Longitude, cancellationToken);

                    if (!string.IsNullOrWhiteSpace(country))
                    {
                        station.Country = country;

                        // Set IsMain for Norwegian stations
                        if (string.Equals(country, "Norway", StringComparison.OrdinalIgnoreCase))
                        {
                            station.IsMain = true;
                        }

                        batchLocated++;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "CountryLocator: Could not determine country for station '{StationName}' (ID: {StationId}, Coords: {Lat}, {Lng})",
                            station.Name, station.StationId, station.Latitude, station.Longitude);
                    }
                }

                // 4. Persist the batch - update only Country and IsMain for geocoded stations
                if (batchLocated > 0)
                {
                    var updatedStations = batch.Where(s => s.Country != null && s.Country != "UKJENT").ToArray();
                    if (updatedStations.Length > 0)
                    {
                        await _weatherStationService.UpdateCountriesAsync(updatedStations, cancellationToken);
                    }
                }

                totalLocated += batchLocated;

                _logger.LogInformation("CountryLocator: Batch {BatchNumber}/{TotalBatches} complete. Located {BatchLocated}/{BatchCount} stations in this batch.",
                    batchNumber, totalBatches, batchLocated, batch.Count);

                // 5. Delay between batches (skip after last batch)
                if (i + BatchSize < stations.Count)
                {
                    await Task.Delay(DelayBetweenBatches, cancellationToken);
                }
            }

            _logger.LogInformation("CountryLocator: Completed. Successfully located country for {TotalLocated}/{TotalStations} stations.",
                totalLocated, stations.Count);

            return totalLocated;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CountryLocator: Operation was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CountryLocator: Error locating countries for weather stations");
            throw;
        }
    }
}
