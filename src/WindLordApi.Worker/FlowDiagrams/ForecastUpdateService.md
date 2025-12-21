# Forecast Update Service Flow

This diagram shows the flow of the forecast update service that runs every 5 minutes.

```mermaid
flowchart TB
    Worker[Worker.cs - Every 5 min] -->|Executes| ForecastUpdateService
    ForecastUpdateService -->|1. Cleanup| DeleteOld[Delete forecasts > 2hrs old]
    ForecastUpdateService -->|2. Get locations| GetLocations[Get up to 50 locations]
    GetLocations -->|Priority 1| NoForecast[Locations without forecast]
    GetLocations -->|Priority 2| OldForecast[Locations with oldest forecast]
    ForecastUpdateService -->|3. Bulk fetch| OpenMeteo[OpenMeteo API - All locations]
    ForecastUpdateService -->|4. For each location| ProcessLoop
    ProcessLoop -->|Fetch takeoff| MetYr1[MetYr API - Takeoff]
    ProcessLoop -->|Combine| Combine[ForecastCombinationService]
    ProcessLoop -->|Fetch landing if exists| MetYr2[MetYr API - Landing]
    ProcessLoop -->|Merge landing data| Merge[Update landing wind fields]
    ProcessLoop -->|Upsert| DB[(PostgreSQL)]
```

## Process Details

1. **Cleanup**: Deletes forecast data older than 2 hours
2. **Location Selection**:
   - Priority 1: Locations without any forecast data
   - Priority 2: Locations with the oldest forecast data
   - Batch size: 50 locations per cycle
3. **Bulk Fetch**: Fetches OpenMeteo data for all 50 locations in a single API call
4. **Per-Location Processing**:
   - Fetch MetYr data for takeoff location
   - Combine OpenMeteo and MetYr data
   - If landing coordinates exist, fetch MetYr data for landing location
   - Merge landing wind data into combined forecasts
   - Upsert all forecast records to database
