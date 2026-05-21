# WindsMobi Provider Sync

## Purpose
This capability owns the WindsMobi-backed weather station sync implemented by `WindsMobiClient`, `WindsMobiMappingService`, and `WindsMobiSyncService`. It polls the WindsMobi station feed as an aggregation surface over multiple upstream provider codes, maps the returned station metadata and latest observations into WindLordApi weather stations and station data, and preserves the rule that weather station metadata is persisted before dependent observations and the latest-station-data projection.

## Requirements

### Requirement: WindsMobi collection SHALL aggregate configured sub-providers with per-provider isolation
The system SHALL call the WindsMobi stations endpoint once for each configured WindsMobi provider code, wait one second between provider requests, and continue with later provider codes when one provider fetch fails. After finishing the fetch loop, it SHALL map the successfully collected stations into one combined `WindsMobiDataResult` containing weather station metadata and station observations.

#### Scenario: One WindsMobi provider fetch fails during aggregation
- **GIVEN** the client is iterating through the configured WindsMobi provider codes
- **WHEN** one provider request throws or returns a non-success status
- **THEN** the client SHALL log a warning for that provider
- **THEN** it SHALL skip that provider's payload
- **THEN** it SHALL continue fetching later provider codes instead of failing the whole aggregation pass

### Requirement: WindsMobi observation mapping SHALL only create station data for stations with usable latest observations
The system SHALL ignore stations that do not have a station id and valid GeoJSON coordinates. It SHALL create `StationData` only for valid stations that also have both a latest observation timestamp and an average wind value.

#### Scenario: A valid WindsMobi observation is normalized for storage
- **GIVEN** a WindsMobi station has a station id, valid coordinates, a `pv-code`, and latest observation fields for `_id` and `w-avg`
- **WHEN** `WindsMobiMappingService` maps the payload
- **THEN** it SHALL produce a `StationData` row for that station

#### Scenario: A station without usable latest observation data is excluded from historical observations
- **GIVEN** a WindsMobi station has valid coordinates but is missing either `last._id` or `last.w-avg`
- **WHEN** `WindsMobiMappingService` maps the payload
- **THEN** it SHALL not produce a `StationData` row for that station
- **THEN** it MAY still produce a `WeatherStation` row if the station metadata itself is valid

### Requirement: WindsMobi observation mapping SHALL normalize latest observations for storage
For each mapped observation, the system SHALL convert wind values from km/h to m/s rounded to one decimal place, normalize wind direction into the range `0..359`, derive `UpdatedAt` from the Unix-seconds observation timestamp, leave `WindMinSpeed` unset, and mark the row as not compressed.

#### Scenario: A mapped WindsMobi observation is normalized into station-data fields
- **GIVEN** a WindsMobi station produces a `StationData` row
- **WHEN** `WindsMobiMappingService` fills the observation fields
- **THEN** the resulting `StationData.UpdatedAt` SHALL be derived from `last._id` as a UTC timestamp
- **THEN** average and gust wind values SHALL be converted from km/h to m/s and rounded to one decimal place
- **THEN** wind direction SHALL be normalized into the range `0..359`

### Requirement: WindsMobi weather-station mapping SHALL preserve upstream station metadata needed by later syncs
The system SHALL map valid WindsMobi station metadata into `WeatherStation` rows by rounding coordinates to five decimal places, using the short name when present or falling back to the station id, initializing `Country` as missing, initializing `IsActive` to `true`, initializing `IsMain` to `false`, and copying the upstream WindsMobi `pv-code` into `WeatherStation.Provider`.

#### Scenario: A valid WindsMobi station is prepared for weather-station upsert
- **GIVEN** a WindsMobi station has a station id, valid coordinates, and a `pv-code`
- **WHEN** `WindsMobiMappingService` maps station metadata
- **THEN** the resulting `WeatherStation.Provider` SHALL copy the station's `pv-code`
- **THEN** the resulting `WeatherStation.Name` SHALL use the short name when present or the station id when it is not
- **THEN** the resulting `WeatherStation` SHALL be initialized with missing country metadata and `IsMain = false`

### Requirement: WindsMobi sync SHALL persist metadata before dependent observations and latest station data
The system SHALL fetch the aggregated WindsMobi result, upsert weather stations before attempting to persist station observations, then upsert historical `StationData`, then derive `LatestStationData` from the same `StationData` array and upsert that projection. If either the mapped weather-station collection or mapped station-observation collection is empty, the sync SHALL log that condition and skip only that empty persistence step.

#### Scenario: A WindsMobi sync persists all three storage layers in order
- **GIVEN** `FetchAllProvidersAsync` returns mapped weather stations and mapped station observations
- **WHEN** `SyncWindsMobiDataAsync` executes successfully
- **THEN** it SHALL upsert `WeatherStation` records before `StationData`
- **THEN** it SHALL derive `LatestStationData` from the same `StationData` array after the historical observation upsert
- **THEN** it SHALL return the count of newly inserted `StationData` rows

#### Scenario: A WindsMobi sync has no mapped weather stations or observations for one layer
- **GIVEN** the aggregated result contains an empty mapped weather-station collection or an empty mapped station-observation collection
- **WHEN** `SyncWindsMobiDataAsync` reaches that persistence step
- **THEN** it SHALL log a warning that there is nothing to upsert for that layer
- **THEN** it SHALL continue processing any non-empty downstream layer that still has data available

### Requirement: Invocation-level sync failures SHALL be logged and rethrown
The system SHALL log and rethrow exceptions that escape `WindsMobiSyncService` after aggregation returns, so worker startup and recurring scheduler orchestration can apply their own failure-isolation rules outside this capability. This SHALL not override the client-level rule that individual provider fetch failures are handled inside the aggregation loop.

#### Scenario: A persistence failure aborts the current WindsMobi sync invocation
- **GIVEN** the client has already returned aggregated WindsMobi data
- **WHEN** a later weather-station, station-data, or latest-station-data persistence step throws
- **THEN** `WindsMobiSyncService` SHALL log `WindsMobi: Error syncing data`
- **THEN** it SHALL rethrow the exception to its caller