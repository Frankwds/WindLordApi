# Forecast Cache Refresh

## Purpose
This capability refreshes forecast cache entries for paragliding locations using MetYr forecast data. The current entry points are the startup job runner in `StartupJobs.RunStartupJobsAsync` and the recurring worker invocation scheduled from `Worker.ExecuteAsync`. The workflow owns forecast refresh orchestration: removing expired forecast rows, choosing which locations to refresh next, fetching takeoff and optional landing forecasts, and persisting the resulting cache entries through the data layer. Shared persistence rules such as composite-key upsert semantics belong to the separate forecast-cache lifecycle capability.

Primary implementation surfaces today are `ForecastUpdateService`, `IParaglidingLocationService`, `IForecastCacheService`, and the MetYr client and mapping services.

## Requirements

### Requirement: Refresh expired and missing coverage first
The system SHALL delete forecast cache rows whose `Time` is more than two hours behind the current UTC time before it selects locations to refresh.

The system SHALL build a refresh batch of at most 50 candidate location ids by taking locations without forecast coverage first and then filling any remaining capacity with locations that have the oldest existing forecast data.

The system SHALL fetch full paragliding-location records through the paragliding-location service before requesting forecasts.

#### Scenario: Locations without forecast coverage fill the batch first
- **GIVEN** active main paragliding locations exist
- **AND** some locations have no forecast cache rows while others only have older forecast data
- **WHEN** the forecast refresh workflow runs
- **THEN** locations without forecast coverage are selected before locations with existing forecast data
- **AND** locations with the oldest existing forecast data are used only to fill the remaining batch capacity

### Requirement: Refresh only active main paragliding locations
The system SHALL refresh forecasts only for locations that resolve to active main paragliding locations.

This filter SHALL still apply when a location id was returned earlier by the location-selection views.

#### Scenario: Non-main or inactive locations are excluded before fetch
- **GIVEN** a candidate location id is present in the refresh selection inputs
- **AND** the underlying paragliding location is inactive or not main
- **WHEN** the workflow materializes locations by id
- **THEN** that location is excluded from forecast fetching
- **AND** no forecast cache rows are upserted for that location in that run

### Requirement: Populate forecast cache from MetYr data
The system SHALL fetch MetYr forecast data for each selected location's takeoff coordinates and map each returned time point to a forecast cache entry for that location.

Each generated entry SHALL set the location id, parsed forecast time, `IsYrData = true`, and the surface forecast fields derived from the MetYr payload.

The workflow SHALL derive `IsDay` from the MetYr symbol code by treating symbol codes containing `night` as night data.

Fields not supplied by the MetYr workflow SHALL remain unset in this capability.

When landing coordinates exist, the system SHOULD fetch landing forecasts and merge landing wind fields into matching takeoff time points before persisting the batch.

#### Scenario: Landing coordinates add landing wind fields
- **GIVEN** a selected active main paragliding location has landing latitude and longitude
- **AND** MetYr returns time-aligned takeoff and landing forecasts for that location
- **WHEN** the forecast refresh workflow runs for that location
- **THEN** the stored forecast cache entries include takeoff forecast conditions
- **AND** matching entries also include landing wind, landing gust, and landing wind direction values

### Requirement: Isolate per-location failures
The system SHALL continue processing the remaining selected locations when forecast fetch, mapping, or persistence fails for one location, and it SHALL log the failed location.

The system SHALL fail the overall refresh when cleanup or location-selection fails before per-location isolation applies.

#### Scenario: One location fails and later locations still refresh
- **GIVEN** the selected batch contains multiple paragliding locations
- **AND** one location fails during forecast retrieval, mapping, or cache upsert
- **WHEN** the workflow runs
- **THEN** that location failure is logged
- **AND** subsequent locations in the batch are still fetched and upserted