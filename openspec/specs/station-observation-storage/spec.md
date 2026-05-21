# Station Observation Storage

## Purpose
This capability persists weather-station observations in two complementary stores. `station_data` keeps the historical observation stream for a weather station, while `latest_station_data` keeps a read-optimized single-row projection per station for the observation that the worker most recently projected as current. Provider sync workflows populate both stores through `StationDataService`, `LatestStationDataService`, and the provider orchestration services in `src/WindLordApi.Worker/Services`.

This capability matters when changing provider sync flows, persistence keys, batching, or the way observation rows are projected into the latest-station-data table. The main implementation entry points are `StationDataRepository`, `LatestStationDataRepository`, `StationDataService`, `LatestStationDataService`, and the provider sync services that call `ConvertFromStationData` before upserting latest rows.

## Requirements

### Requirement: Historical observations remain idempotent by station and timestamp
The system SHALL persist historical station observations in `station_data` using `StationId` and `UpdatedAt` as the uniqueness boundary.

The system SHALL treat duplicate observations for the same `StationId` and `UpdatedAt` as idempotent inserts and SHALL NOT overwrite an existing historical row when the same key is seen again.

Provider workflows SHALL ensure weather-station metadata exists before inserting dependent observation rows because `station_data.StationId` is constrained to an existing weather station.

#### Scenario: Duplicate observation is received again
- **GIVEN** a `station_data` row already exists for a weather station at a specific `UpdatedAt`
- **WHEN** a provider sync submits another `StationData` row with the same `StationId` and `UpdatedAt`
- **THEN** the repository matches on `(StationId, UpdatedAt)`
- **AND** the existing historical row remains unchanged
- **AND** the insert is counted as idempotent rather than as a second observation row

#### Scenario: Provider sync persists observations after station metadata
- **GIVEN** a provider workflow has fetched weather-station metadata and observation data in the same run
- **WHEN** it persists the results
- **THEN** it upserts weather-station metadata before inserting `station_data`
- **AND** the observation insert can satisfy the foreign-key relationship on `StationId`

### Requirement: Latest station data is a single-row projection per station
The system SHALL store at most one `latest_station_data` row per `StationId`.

The system SHALL derive latest-station-data rows from `StationData` by copying `StationId`, `WindSpeed`, `WindGust`, `WindMinSpeed`, `Direction`, `Temperature`, and `UpdatedAt` and by omitting `IsCompressed`.

The system SHALL upsert `latest_station_data` on `StationId` only. When a row for that station already exists, the repository updates the stored projection with the incoming values.

Current implementation note: the repository does not compare timestamps when updating `latest_station_data`; worker sync services are expected to project the observation they intend to expose as latest.

#### Scenario: Provider sync projects a stored observation into latest data
- **GIVEN** a provider sync has a batch of `StationData` rows ready for persistence
- **WHEN** the workflow calls `LatestStationDataService.ConvertFromStationData`
- **THEN** each projected row copies the shared observation fields from `StationData`
- **AND** the projection excludes `IsCompressed`
- **AND** the workflow upserts the projection into `latest_station_data`

#### Scenario: A latest row already exists for the station
- **GIVEN** `latest_station_data` already contains a row for a weather station
- **WHEN** the repository upserts another `LatestStationData` row for the same `StationId`
- **THEN** the repository matches on `StationId`
- **AND** it updates the stored row with the incoming observation fields
- **AND** it preserves the existing primary key row identity rather than creating a second latest row

### Requirement: Observation persistence is batched and transactional
The system SHALL reject `null`, empty, or all-null arrays passed to `StationDataService.UpsertManyAsync` or `LatestStationDataService.UpsertManyAsync`.

The system SHALL process station-observation and latest-station-data writes in batches of 1000 records.

The system SHALL wrap each batch in an explicit unit-of-work transaction, commit successful batches, and roll back the batch if repository persistence fails.

#### Scenario: Caller submits invalid input to an observation service
- **GIVEN** a caller passes a `null`, empty, or all-null observation array to one of the storage services
- **WHEN** the service validates the input
- **THEN** it throws an `ArgumentException`
- **AND** it does not begin persistence work for that request

#### Scenario: A batch fails during persistence
- **GIVEN** a storage service has begun a transaction for a batch of observation rows
- **WHEN** repository persistence throws an exception
- **THEN** the service rolls back that transaction
- **AND** it logs the batch failure
- **AND** it rethrows the exception to the caller