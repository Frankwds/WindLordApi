# MetYr Forecast Refresh

## Purpose
This capability documents the authoritative MetYr workflow that refreshes forecast cache entries for paragliding locations. The current entry points are the startup job runner in `StartupJobs.RunStartupJobsAsync` and the recurring worker invocation scheduled from `Worker.ExecuteAsync`. This workflow owns expired-row cleanup, selecting locations with missing or oldest forecast coverage, fetching takeoff and optional landing forecasts, and persisting the resulting Yr-backed cache entries through the data layer. Shared persistence rules such as composite-key upsert semantics and provider precedence belong to the separate forecast-cache lifecycle capability.

Primary implementation surfaces today are `MetYrForecastRefreshService`, `IParaglidingLocationService`, `IForecastCacheService`, and the MetYr client and mapping services.

## Requirements

### Requirement: Refresh expired and missing coverage first
The authoritative MetYr refresh workflow SHALL delete forecast cache rows whose `Time` is more than two hours behind the current UTC time before it selects locations to refresh.

The MetYr refresh workflow SHALL build a refresh batch of at most 50 candidate location ids by taking locations without forecast coverage first and then filling any remaining capacity with locations that have the oldest existing forecast data.

The MetYr refresh workflow SHALL fetch full paragliding-location records through the paragliding-location service before requesting forecasts.

#### Scenario: Locations without forecast coverage fill the MetYr batch first
- **GIVEN** active main paragliding locations exist
- **AND** some locations have no forecast cache rows while others only have older forecast data
- **WHEN** the authoritative MetYr refresh workflow runs
- **THEN** locations without forecast coverage are selected before locations with existing forecast data
- **AND** locations with the oldest existing forecast data are used only to fill the remaining batch capacity

### Requirement: Refresh only active main paragliding locations
The MetYr refresh workflow SHALL refresh forecasts only for locations that resolve to active main paragliding locations.

This filter SHALL still apply when a location id was returned earlier by the location-selection queries.

#### Scenario: Non-main or inactive locations are excluded before fetch
- **GIVEN** a candidate location id is present in the refresh selection inputs
- **AND** the underlying paragliding location is inactive or not main
- **WHEN** the workflow materializes locations by id
- **THEN** that location is excluded from forecast fetching
- **AND** no forecast cache rows are upserted for that location in that run

### Requirement: Populate forecast cache from MetYr data
The authoritative MetYr refresh workflow SHALL fetch MetYr forecast data for each selected location's takeoff coordinates and map each returned time point to a forecast cache entry for that location.

Each Yr-derived entry SHALL set the location id, parsed forecast time, `IsYrData = true`, and the surface forecast fields derived from the MetYr payload.

The workflow SHALL derive `IsDay` from the MetYr symbol code by treating symbol codes containing `night` as night data.

When landing coordinates exist, the system SHOULD fetch landing forecasts from MetYr and merge landing wind fields into matching takeoff time points before persisting the batch.

#### Scenario: Landing coordinates add landing wind fields
- **GIVEN** a selected active main paragliding location has landing latitude and longitude
- **AND** MetYr returns time-aligned takeoff and landing forecasts for that location
- **WHEN** the authoritative MetYr refresh workflow runs for that location
- **THEN** the stored forecast cache entries include takeoff forecast conditions
- **AND** matching entries also include landing wind, landing gust, and landing wind direction values

### Requirement: Per-location failures do not abort the MetYr batch
The authoritative MetYr refresh workflow SHALL continue processing the remaining selected locations when forecast fetch, mapping, or persistence fails for one location, and it SHALL log the failed location.

The authoritative MetYr refresh workflow SHALL fail the overall run when cleanup or location-selection fails before per-location isolation applies.

#### Scenario: One MetYr location fails and later locations still refresh
- **GIVEN** the selected MetYr batch contains multiple paragliding locations
- **AND** one location fails during forecast retrieval, mapping, or cache upsert
- **WHEN** the authoritative MetYr refresh workflow runs
- **THEN** that location failure is logged
- **AND** subsequent locations in the batch are still fetched and upserted