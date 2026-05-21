# Paragliding Location Queries

## Purpose
This capability documents the read-side query contract that forecast refresh uses to choose which paragliding locations should receive forecast updates next. It exists to separate location-selection rules from forecast fetching and persistence, and to preserve the database-backed prioritization behavior that the worker depends on.

This capability is implemented primarily in `ParaglidingLocationRepository`, the `locations_without_forecast` and `locations_with_oldest_forecast` database views, and the `ParaglidingLocationService` pass-through used by `ForecastUpdateService`. It does not include forecast fetching or forecast-cache persistence beyond the location-selection behavior those workflows depend on.

## Requirements

### Requirement: Locations without forecasts are exposed through a database-backed priority query
The system SHALL expose paragliding locations without any forecast rows through the `locations_without_forecast` database view and return them ordered by location name with an explicit result limit.

The current implementation defines this query over `all_paragliding_locations` with a left join to `forecast_cache`, and includes only rows where the location is active, marked as main, and has no matching forecast row.

#### Scenario: Querying locations without forecasts returns active main locations only
- **GIVEN** a paragliding location is active, marked as main, and has no rows in `forecast_cache`
- **WHEN** `GetLocationsWithoutForecastAsync(limit, ...)` is called with a limit that includes that location
- **THEN** the location is returned from the `locations_without_forecast` view
- **AND** the result includes its `LocationId`, `Name`, `Latitude`, and `Longitude`

#### Scenario: Locations without forecasts are name-ordered and limited
- **GIVEN** multiple active main paragliding locations have no forecast rows
- **WHEN** `GetLocationsWithoutForecastAsync(limit, ...)` is called
- **THEN** the repository orders those view rows by `Name`
- **AND** it returns no more than `limit` rows

### Requirement: Locations with the oldest forecast data are exposed as a secondary priority query
The system SHALL expose paragliding locations that already have forecast rows through the `locations_with_oldest_forecast` database view and return them ordered by the oldest `updated_at` value with an explicit result limit.

The current implementation defines this view by joining `forecast_cache` to `all_paragliding_locations`, grouping by `location_id`, and restricting the view to locations where `IsMain` is true.

#### Scenario: Querying stale forecast coverage ranks by oldest update time
- **GIVEN** multiple main paragliding locations have forecast rows in `forecast_cache`
- **WHEN** `GetLocationsWithOldestForecastAsync(limit, ...)` is called
- **THEN** the repository orders those view rows by `OldestUpdatedAt`
- **AND** it returns no more than `limit` rows

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

### Requirement: Forecast refresh consumes query results in a two-stage priority flow
The system SHALL let forecast refresh fill its location batch by taking IDs from `GetLocationsWithoutForecastAsync(...)` first, then filling any remaining slots from `GetLocationsWithOldestForecastAsync(...)`, and finally materializing full `ParaglidingLocation` rows through `GetByIdsAsync(...)`.

This means the final batch ultimately processes only active main locations, because full location materialization applies the active-main filter even after IDs are selected from the priority views.

#### Scenario: Locations without forecast coverage are prioritized before stale coverage
- **GIVEN** forecast refresh is selecting up to its configured batch size of paragliding locations
- **WHEN** it builds the next batch of location IDs
- **THEN** it first adds IDs returned by `GetLocationsWithoutForecastAsync(...)`
- **AND** it only queries `GetLocationsWithOldestForecastAsync(...)` if there are remaining slots after that first query
- **AND** it materializes the final batch through `GetByIdsAsync(...)` before any forecast fetch begins