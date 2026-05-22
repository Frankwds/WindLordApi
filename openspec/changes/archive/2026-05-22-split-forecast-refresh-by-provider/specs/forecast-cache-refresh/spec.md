## MODIFIED Requirements

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

### Requirement: Isolate provider workflow failures
The authoritative MetYr refresh workflow SHALL continue processing the remaining selected locations when forecast fetch, mapping, or persistence fails for one location, and it SHALL log the failed location.

The authoritative MetYr refresh workflow SHALL fail the overall run when cleanup or location-selection fails before per-location isolation applies.

The Open-Meteo supplement workflow SHALL log batch-level request, mapping, or persistence failures once per failed run and SHALL not block later MetYr refresh runs from persisting authoritative rows on their normal cadence.

#### Scenario: One MetYr location fails and later locations still refresh
- **GIVEN** the selected MetYr batch contains multiple paragliding locations
- **AND** one location fails during forecast retrieval, mapping, or cache upsert
- **WHEN** the authoritative MetYr refresh workflow runs
- **THEN** that location failure is logged
- **AND** subsequent locations in the batch are still fetched and upserted

#### Scenario: Open-Meteo batch failure does not stop later MetYr refreshes
- **GIVEN** the Open-Meteo supplement workflow encounters a batch-level failure
- **WHEN** later authoritative MetYr refresh runs occur on schedule
- **THEN** those MetYr runs still fetch and persist Yr-derived forecast rows
- **AND** the Open-Meteo failure is logged without transferring authority to Open-Meteo

## ADDED Requirements

### Requirement: Open-Meteo supplementation SHALL run as a separate provider-owned workflow
The system SHALL run Open-Meteo forecast supplementation as a workflow separate from the authoritative MetYr refresh workflow.

The Open-Meteo supplement workflow SHALL issue one batched takeoff-forecast request for the selected locations in that workflow run, map the returned rows as Open-Meteo-backed forecast entries, and persist them only through the shared forecast-cache repository contract.

Open-Meteo-supplemented rows SHALL set `IsYrData = false`, SHALL populate only the currently persisted takeoff surface fields other than wind gusts, SHALL round mapped numeric values to match destination forecast-cache precision, and SHALL leave landing forecast fields plus any unsupported or unavailable fields unset in this capability.

#### Scenario: Open-Meteo writes takeoff-only supplemental rows
- **GIVEN** the Open-Meteo supplement workflow selects active main paragliding locations for a batch
- **WHEN** it persists mapped Open-Meteo forecast rows for those locations
- **THEN** the persisted rows are marked with `IsYrData = false`
- **AND** landing forecast fields remain unset for those rows

### Requirement: Open-Meteo supplement selection SHALL prioritize the shortest Open-Meteo forecast tail
The Open-Meteo supplement workflow SHALL choose candidate locations through one ordering that uses the latest Open-Meteo-backed forecast timestamp as the tail signal.

Locations with no Open-Meteo-backed forecast rows SHALL sort ahead of locations that already have Open-Meteo-backed coverage.

When multiple eligible locations already have Open-Meteo-backed coverage, the workflow SHALL prefer the locations whose latest Open-Meteo-backed forecast timestamp is earliest.

This priority SHALL be based on Open-Meteo-backed forecast horizon rather than generic forecast freshness or `UpdatedAt` values from Yr-backed rows.

#### Scenario: Locations without Open-Meteo coverage have the shortest tail
- **GIVEN** active main paragliding locations exist
- **AND** some locations have no Open-Meteo-backed forecast rows while others have existing Open-Meteo-backed rows
- **WHEN** the Open-Meteo supplement workflow selects its next batch
- **THEN** locations with no Open-Meteo-backed forecast rows are selected before locations whose Open-Meteo-backed forecast horizon already extends into the future

#### Scenario: Shorter Open-Meteo horizons are selected before longer horizons
- **GIVEN** multiple active main paragliding locations already have Open-Meteo-backed forecast rows
- **AND** those locations have different latest Open-Meteo-backed forecast timestamps
- **WHEN** the Open-Meteo supplement workflow selects its next batch
- **THEN** the location whose latest Open-Meteo-backed forecast timestamp is earliest is selected before locations with later Open-Meteo-backed forecast timestamps