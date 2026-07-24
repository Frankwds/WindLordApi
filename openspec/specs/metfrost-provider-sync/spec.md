# MetFrost Provider Sync

## Purpose

This capability synchronizes MET Frost weather-station metadata and latest
observations into WindLordApi's weather-station stores. It exists to keep MET
provider stations discoverable, keep active MET stations flowing into historical
and latest station-data tables, and periodically reconcile station activity
based on whether persisted observations exist. The controlling implementation
starts in `WindLordApi.Worker.Services.MetFrostSyncService`, uses
`WindLordApi.Integrations.MetFrost` for API access and provider-specific
mapping, and relies on shared weather-station, station-data, and
latest-station-data services in `WindLordApi.Data` for persistence.

## Requirements

### Requirement: Sync active MET observations in bounded batches

The system SHALL fetch latest observations only for weather stations whose
provider is `MET` and whose current persisted `IsActive` flag is `true`.

The system SHALL batch MET observation requests in groups of at most 100 station
ids.

The system SHALL map fetched observations into `StationData` records, insert
only new historical observations keyed by `(StationId, UpdatedAt)`, and upsert
the corresponding `LatestStationData` projection keyed by `StationId`.

The system SHALL continue processing later batches when one MET observation
batch fails.

#### Scenario: Active station observations create historical and latest records

- **GIVEN** persisted weather stations for provider `MET` are marked active
- **WHEN** `SyncLatestStationDataAsync` runs
- **THEN** it requests MET observations for those station ids in batches of no
  more than 100
- **AND** it maps valid observations into `StationData`
- **AND** it inserts only observations whose `(StationId, UpdatedAt)` pair does
  not already exist
- **AND** it upserts `LatestStationData` rows for the same stations using the
  mapped observation values

#### Scenario: One observation batch fails without stopping the rest of the sync

- **GIVEN** multiple batches of active MET station ids must be synchronized
- **WHEN** one batch throws while fetching or mapping MET observations
- **THEN** that batch failure is logged with the batch number and station ids
- **AND** later batches are still attempted

### Requirement: Map MET observations using provider-specific rules

The system SHALL derive the persisted station id from the MET `sourceId` by
removing any sensor suffix after `:`.

The system SHALL group MET observations by derived station id and keep the
latest `referenceTime` seen for that station as the persisted timestamp.

The system SHALL populate wind gust from `max(wind_speed_of_gust PT10M)` when
present and SHALL fall back to `max(wind_speed_of_gust PT1H)` only when the
10-minute gust is absent.

The system SHALL discard mapped observation records unless they contain at least
one persisted wind-speed metric: `wind_speed`, `max(wind_speed_of_gust PT10M)`,
`max(wind_speed_of_gust PT1H)`, or a future provider value that maps to
`wind_min_speed`.

The system SHALL normalize persisted direction into the inclusive `0` to `359`
range.

#### Scenario: Sensor-specific MET observation ids collapse into one station record

- **GIVEN** MET returns observations for a source id such as `SN12345:0`
- **WHEN** the observations are mapped to station data
- **THEN** the persisted `StationId` is `SN12345`
- **AND** observations for the same station id are combined into a single mapped
  record for that sync batch

#### Scenario: Gust resolution prefers 10-minute data

- **GIVEN** a MET observation payload contains both
  `max(wind_speed_of_gust PT10M)` and `max(wind_speed_of_gust PT1H)` for a
  station
- **WHEN** the payload is mapped
- **THEN** the persisted gust value uses the 10-minute gust
- **AND** the hourly gust is used only if the 10-minute gust is unavailable

#### Scenario: Observations with no persisted speed metric are not stored

- **GIVEN** a MET observation group lacks wind speed, gust, and minimum wind
  speed
- **WHEN** the group is mapped to `StationData`
- **THEN** no `StationData` or `LatestStationData` row is produced for that
  station from that group

#### Scenario: Partial observations are still persisted

- **GIVEN** a MET observation group contains a gust value but no wind speed or
  wind direction
- **WHEN** the group is mapped to `StationData`
- **THEN** a `StationData` and `LatestStationData` row is produced for that
  station from that group
- **AND** the missing wind speed and wind direction values remain null in
  persistence

#### Scenario: Direction without a speed metric is dropped

- **GIVEN** a MET observation group contains wind direction but no wind speed,
  gust, or minimum wind speed
- **WHEN** the group is mapped to `StationData`
- **THEN** no `StationData` or `LatestStationData` row is produced for that
  station from that group

### Requirement: Refresh MET weather-station catalog without taking ownership of active-state reconciliation

The system SHALL fetch the full MET station catalog from the MET sources
endpoint and map only stations with valid two-coordinate geometry and non-zero
latitude and longitude inside legal geographic bounds.

The system SHALL map MET stations into `WeatherStation` records with provider
`MET`, round latitude and longitude to four decimals, round altitude to an
integer, default missing country to `UKJENT`, and mark `IsMain` only when the
station country is `Norge`.

The system SHALL upsert mapped MET weather stations by `StationId`.

The system SHALL NOT rely on weather-station catalog upsert to change the
persisted `IsActive` value of existing MET stations.

#### Scenario: Station catalog sync filters invalid geometry

- **GIVEN** the MET sources response contains stations with missing geometry,
  malformed coordinates, zero coordinates, or out-of-range coordinates
- **WHEN** `SyncWeatherStationsAsync` runs
- **THEN** those stations are excluded from the mapped weather-station set
- **AND** only valid MET stations are upserted

#### Scenario: Existing MET active state survives catalog refresh

- **GIVEN** an existing `MET` weather station is currently inactive in
  persistence
- **AND** the MET catalog response still includes that station
- **WHEN** `SyncWeatherStationsAsync` upserts the mapped station catalog
- **THEN** the station metadata is refreshed
- **BUT** the persisted `IsActive` flag remains unchanged until the
  active-status workflow reconciles it

### Requirement: Reconcile MET active status from persisted observation presence

The system SHALL perform MET active-status reconciliation as a separate workflow
from station catalog refresh.

The system SHALL begin active-status reconciliation by synchronizing
observations for currently inactive `MET` stations.

The system SHALL activate inactive `MET` stations that now have persisted
`StationData`.

The system SHALL deactivate active `MET` stations that have no persisted
`StationData`.

#### Scenario: Inactive station becomes active after observation data appears

- **GIVEN** a persisted `MET` weather station is inactive
- **AND** the inactive-station observation sync inserts station data for that
  station
- **WHEN** `SyncWeatherStationsActiveStatusAsync` continues its reconciliation
  steps
- **THEN** the station is marked active

#### Scenario: Active station without persisted data becomes inactive

- **GIVEN** a persisted `MET` weather station is active
- **AND** no `StationData` exists for that station
- **WHEN** `SyncWeatherStationsActiveStatusAsync` runs its deactivation step
- **THEN** the station is marked inactive

### Requirement: Startup and schedule entry points remain separate but consistent

The system SHALL expose three distinct MET Frost workflows through
`IMetFrostSyncService`: latest observation sync, station catalog sync, and
active-status sync.

The worker SHOULD execute all three workflows once during startup.

The worker SHALL schedule the MET latest observation sync every five minutes at
minute offset `2`, the MET station catalog sync every Sunday at `03:00` UTC, and
the MET active-status sync every Sunday at `04:00` UTC.

#### Scenario: Startup runs all MET workflows once

- **GIVEN** the worker host starts successfully
- **WHEN** startup jobs are executed
- **THEN** MET latest observation sync runs once
- **AND** MET station catalog sync runs once
- **AND** MET active-status sync runs once

#### Scenario: Scheduled execution keeps workflows distinct

- **GIVEN** the worker has entered its recurring schedule loop
- **WHEN** the configured MET cron schedules are reached
- **THEN** active observation sync, station catalog refresh, and active-status
  reconciliation are triggered as separate scheduled jobs rather than one
  combined workflow
