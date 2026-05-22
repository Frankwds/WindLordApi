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

Each Yr-derived entry SHALL set the location id, parsed forecast time, `IsYrData = true`, and the surface forecast fields derived from the MetYr payload.

The workflow SHALL derive `IsDay` from the MetYr symbol code by treating symbol codes containing `night` as night data.

When landing coordinates exist, the system SHOULD fetch landing forecasts from MetYr and merge landing wind fields into matching takeoff time points before persisting the batch.

The workflow SHALL issue one batched Open-Meteo takeoff forecast request for all selected locations in the current refresh batch, using takeoff coordinates truncated to three decimals. For each location whose MetYr fetch succeeds, the workflow SHALL append only Open-Meteo rows whose timestamp is strictly later than the latest MetYr timestamp returned for that location in the current run.

Open-Meteo-supplemented rows SHALL set `IsYrData = false`, SHALL populate only the currently persisted takeoff surface fields, SHALL round mapped numeric values to match the destination forecast-cache precision, and SHALL leave unsupported or unavailable fields unset in this capability.

#### Scenario: Landing coordinates add landing wind fields
- **GIVEN** a selected active main paragliding location has landing latitude and longitude
- **AND** MetYr returns time-aligned takeoff and landing forecasts for that location
- **WHEN** the forecast refresh workflow runs for that location
- **THEN** the stored forecast cache entries include takeoff forecast conditions
- **AND** matching entries also include landing wind, landing gust, and landing wind direction values

#### Scenario: Open-Meteo supplements only later timestamps
- **GIVEN** a selected active main paragliding location has MetYr takeoff forecast rows in the current refresh run
- **AND** the batched Open-Meteo response contains forecast rows for that same location
- **WHEN** the forecast refresh workflow merges provider data for that location
- **THEN** all MetYr rows are retained for that location
- **AND** only Open-Meteo rows whose timestamp is strictly later than the latest MetYr timestamp are appended
- **AND** appended Open-Meteo rows set `IsYrData` to `false`

### Requirement: Isolate per-location failures
The system SHALL continue processing the remaining selected locations when forecast fetch, mapping, or persistence fails for one location, and it SHALL log the failed location.

The system SHALL fail the overall refresh when cleanup or location-selection fails before per-location isolation applies.

If the batched Open-Meteo request fails, is partial, or is otherwise unusable after locations have been selected, the workflow SHALL still persist the MetYr-derived forecast rows for locations whose MetYr processing succeeded.

If MetYr fails for a location, the workflow SHALL skip persistence for that location even if Open-Meteo returned supplemental rows for it.

#### Scenario: One location fails and later locations still refresh
- **GIVEN** the selected batch contains multiple paragliding locations
- **AND** one location fails during forecast retrieval, mapping, or cache upsert
- **WHEN** the workflow runs
- **THEN** that location failure is logged
- **AND** subsequent locations in the batch are still fetched and upserted

#### Scenario: Batched Open-Meteo failure still persists Yr rows
- **GIVEN** the selected batch contains multiple paragliding locations
- **AND** the batched Open-Meteo request fails, is partial, or is otherwise unusable
- **AND** one or more locations still return valid MetYr forecast data
- **WHEN** the workflow completes
- **THEN** the affected locations persist their MetYr-derived forecast rows
- **AND** the Open-Meteo batch failure is logged without preventing those MetYr writes

#### Scenario: MetYr failure prevents location persistence even when Open-Meteo succeeded
- **GIVEN** the selected batch contains a paragliding location
- **AND** the batched Open-Meteo request returned supplemental rows for that location
- **AND** the MetYr fetch or MetYr mapping for that location fails
- **WHEN** the workflow completes
- **THEN** no forecast cache rows are upserted for that location in that run

### Requirement: Batched Open-Meteo responses SHALL correlate predictably to selected locations
The system SHALL correlate each Open-Meteo response block back to the selected paragliding location set without relying exclusively on a provider-supplied `location_id` field.

Request order SHALL be the primary correlation key, and returned coordinates SHALL be used as a sanity check for the corresponding location after both sides have been normalized to the truncated three-decimal request precision.

#### Scenario: Request order correlates batched response blocks
- **GIVEN** a forecast refresh batch contains multiple selected takeoff locations
- **AND** the workflow submits those locations to Open-Meteo in a defined request order
- **WHEN** the Open-Meteo response returns one forecast block per requested coordinate pair
- **THEN** each response block is matched back to the location at the same request position
- **AND** the returned coordinates are checked against that location before rows are merged

### Requirement: Open-Meteo weather normalization SHALL map WMO codes into the existing app vocabulary
The system SHALL map Open-Meteo WMO `weather_code` values plus `is_day` into the Yr-compatible weather-code vocabulary consumed by WindLord.

The mapping SHALL use `is_day` only for WMO codes `0`, `1`, and `2`. Unknown or unsupported WMO codes SHALL remain unset rather than being coerced into an unrelated target code.

#### Scenario: Day and night variants are chosen only for supported codes
- **GIVEN** an Open-Meteo hourly row has WMO code `0`, `1`, or `2`
- **AND** the row includes an `is_day` value
- **WHEN** the row is normalized into a forecast cache entry
- **THEN** the weather code uses the matching day or night variant from the existing app vocabulary

#### Scenario: Unsupported WMO codes remain unset
- **GIVEN** an Open-Meteo hourly row has a WMO weather code that is not supported by the locked mapping table
- **WHEN** the row is normalized into a forecast cache entry
- **THEN** the stored weather code remains unset

