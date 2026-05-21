# Forecast Cache Lifecycle

## Purpose
This capability documents the persistence contract for forecast cache rows that back forecast lookups for paragliding locations. It exists to preserve idempotent writes, retention cleanup behavior, and the batching and transaction rules that the worker's forecast refresh workflow depends on.

This capability begins in `ForecastCacheService` and `ForecastCacheRepository`, with schema constraints defined in `ApplicationDbContext`. It does not define which locations are selected for refresh, when refresh runs, or why a particular cleanup cutoff is chosen; those behaviors belong to the forecast-refresh workflow.

## Requirements

### Requirement: Forecast cache rows are unique by location and forecast time
The system SHALL identify forecast cache rows by the pair of paragliding location and forecast time.

When a write targets an existing `(LocationId, Time)` pair, the system SHALL update the existing row's mutable forecast values instead of creating a duplicate row.

The current upsert projection SHOULD preserve the existing row identity and creation timestamp because it updates forecast payload fields and `UpdatedAt`, but does not replace `Id`, `LocationId`, `Time`, or `CreatedAt`.

#### Scenario: Upsert refreshes an existing forecast row
- **GIVEN** `forecast_cache` already contains a row for a paragliding location at a specific forecast time
- **WHEN** the repository upserts a forecast for the same `LocationId` and `Time`
- **THEN** exactly one row SHALL remain for that key
- **AND** the stored forecast payload SHALL reflect the incoming values rather than the previous values

#### Scenario: Same forecast time across different locations does not conflict
- **GIVEN** two paragliding locations share the same forecast timestamp
- **WHEN** the repository upserts one forecast row for each location at that time
- **THEN** both rows SHALL be stored because the uniqueness rule is scoped to `(LocationId, Time)`

### Requirement: Forecast cache writes are validated and processed in transactional batches
The forecast cache service SHALL reject a null or empty forecast array.

The forecast cache service SHALL reject an array that contains only null elements.

When the array contains both valid rows and null elements, the forecast cache service SHALL ignore the null elements and persist the valid rows.

The forecast cache service SHALL process writes in batches of 1000 rows and SHALL wrap each batch in an explicit database transaction.

#### Scenario: Large forecast writes are split across batches
- **GIVEN** more than 1000 valid forecast rows for persisted paragliding locations
- **WHEN** the forecast cache service upserts the rows
- **THEN** it SHALL persist all rows across one or more 1000-row batches
- **AND** each batch SHALL run inside its own explicit transaction

#### Scenario: Mixed null and valid rows still persist valid forecasts
- **GIVEN** an upsert request that contains valid forecast rows and null elements
- **WHEN** the forecast cache service processes the request
- **THEN** it SHALL persist the valid rows
- **AND** it SHALL not attempt to write the null elements

### Requirement: Matching forecast rows replace the mutable forecast payload
For an existing `(LocationId, Time)` row, the repository SHALL replace the stored mutable forecast payload with the incoming payload during upsert.

The mutable payload currently includes surface conditions, landing conditions, atmospheric fields, precipitation bounds, `IsYrData`, and `UpdatedAt`.

Database constraints SHALL continue to enforce the shared persistence contract for these rows, including the `(LocationId, Time)` alternate key and the rule that `IsDay` can only be `0` or `1`.

#### Scenario: Upsert updates the full forecast payload for an existing key
- **GIVEN** a stored forecast row already exists for a location and forecast time
- **WHEN** an incoming row for that same key changes forecast payload fields such as temperature, wind, or direction
- **THEN** the stored row SHALL be updated with the incoming forecast payload
- **AND** the write SHALL not create a second row for that key

### Requirement: Forecast cleanup removes only rows older than the supplied cutoff
The forecast cache repository SHALL delete forecast rows whose forecast time is strictly earlier than the supplied cutoff time.

Rows at the cutoff time or later SHALL remain.

Cleanup SHALL apply across all locations rather than only a single location.

#### Scenario: Cleanup preserves rows at and after the cutoff
- **GIVEN** stored forecast rows before, at, and after a cutoff time
- **WHEN** old forecasts are deleted using that cutoff
- **THEN** only rows earlier than the cutoff SHALL be removed
- **AND** rows exactly at the cutoff and after the cutoff SHALL remain

#### Scenario: Cleanup deletes outdated rows across locations
- **GIVEN** multiple paragliding locations each have outdated and current forecast rows
- **WHEN** old forecasts are deleted
- **THEN** outdated rows SHALL be removed for every location
- **AND** current rows SHALL remain regardless of location