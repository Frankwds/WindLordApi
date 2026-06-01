# Paragliding Location Queries

## Purpose
This capability documents the read-side query contract that forecast workflows use to choose which paragliding locations should receive updates next. It exists to separate location-selection rules from forecast fetching and persistence, and to preserve the database-backed prioritization behavior that the worker depends on.

This capability is implemented primarily in `ParaglidingLocationRepository`, the inline MetYr and Open-Meteo refresh-candidate queries, and the `ParaglidingLocationService` pass-through used by `MetYrForecastRefreshService` and `OpenMeteoForecastSupplementService`. It does not include forecast fetching or forecast-cache persistence beyond the location-selection behavior those workflows depend on.

## Requirements

### Requirement: MetYr refresh candidates are selected by one inline priority query flow
The system SHALL expose authoritative MetYr refresh candidates through inline repository queries instead of database views.

The repository SHALL first select active main paragliding locations with no forecast rows, ordered by location name and limited by the requested batch size.

If capacity remains, the repository SHALL fill the remaining slots with active main paragliding locations whose oldest forecast `updated_at` value is earliest.

#### Scenario: Locations without forecasts are prioritized first for MetYr refresh
- **GIVEN** active main paragliding locations exist
- **AND** some locations have no rows in `forecast_cache`
- **WHEN** `GetMetYrRefreshCandidatesAsync(limit, ...)` is called
- **THEN** those locations without forecast rows are selected before any locations with existing forecast coverage
- **AND** the repository returns no more than `limit` location IDs

#### Scenario: Stale forecast coverage fills remaining MetYr capacity
- **GIVEN** active main paragliding locations with no forecast rows do not fill the requested batch size
- **AND** other active main paragliding locations already have forecast rows in `forecast_cache`
- **WHEN** `GetMetYrRefreshCandidatesAsync(limit, ...)` is called
- **THEN** the repository fills the remaining slots with locations ordered by the earliest oldest forecast `updated_at` value
- **AND** the combined result still returns no more than `limit` location IDs

### Requirement: Full location materialization enforces active main filtering
The system SHALL materialize full paragliding-location records only from `all_paragliding_locations` rows whose IDs were requested and whose `IsActive` and `IsMain` flags are both true.

This filter is applied in `GetByIdsAsync(...)` even if an earlier priority query produced an ID that no longer satisfies those flags.

#### Scenario: Inactive or secondary locations are excluded during materialization
- **GIVEN** a requested location ID exists in `all_paragliding_locations`
- **AND** that location is inactive or not marked as main
- **WHEN** `GetByIdsAsync(ids, ...)` is called
- **THEN** that location is not returned in the materialized `ParaglidingLocation` result set

#### Scenario: Empty ID requests short-circuit to no locations
- **GIVEN** the caller provides no location IDs
- **WHEN** `GetByIdsAsync(ids, ...)` is called
- **THEN** the repository returns an empty result without querying for any location rows

### Requirement: MetYr refresh consumes prioritized candidate IDs before materialization
The system SHALL let the authoritative MetYr refresh workflow fill its location batch by taking IDs from `GetMetYrRefreshCandidatesAsync(...)` and then materializing full `ParaglidingLocation` rows through `GetByIdsAsync(...)`.

This means the final batch ultimately processes only active main locations, because full location materialization applies the active-main filter even after IDs are selected by the priority queries.

#### Scenario: Locations without forecast coverage are prioritized before stale coverage
- **GIVEN** the authoritative MetYr refresh workflow is selecting up to its configured batch size of paragliding locations
- **WHEN** it builds the next batch of location IDs
- **THEN** it receives IDs selected with locations without forecast coverage ahead of locations with stale forecast coverage
- **AND** it materializes the final batch through `GetByIdsAsync(...)` before any forecast fetch begins

### Requirement: Open-Meteo refresh candidates are ordered by the shortest Open-Meteo forecast tail
The system SHALL expose Open-Meteo refresh candidates through one query that orders active main paragliding locations by the latest Open-Meteo-backed forecast timestamp for each location.

Locations with no Open-Meteo-backed forecast rows SHALL sort ahead of locations that already have Open-Meteo-backed coverage.

When multiple locations already have Open-Meteo-backed coverage, the query SHALL prefer the locations whose latest Open-Meteo-backed forecast timestamp is earliest.

#### Scenario: Locations without Open-Meteo rows are returned first
- **GIVEN** active main paragliding locations exist
- **AND** some locations have no Open-Meteo-backed forecast rows while others have Open-Meteo-backed rows
- **WHEN** `GetOpenMeteoRefreshCandidatesAsync(limit, ...)` is called
- **THEN** locations with no Open-Meteo-backed forecast rows are ordered ahead of locations with existing Open-Meteo-backed coverage

#### Scenario: Shorter Open-Meteo tails are prioritized before longer ones
- **GIVEN** multiple active main paragliding locations already have Open-Meteo-backed forecast rows
- **AND** those locations have different latest Open-Meteo-backed forecast timestamps
- **WHEN** `GetOpenMeteoRefreshCandidatesAsync(limit, ...)` is called
- **THEN** the repository orders those locations by the earliest latest Open-Meteo-backed forecast timestamp
- **AND** it returns no more than `limit` rows