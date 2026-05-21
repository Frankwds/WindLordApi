# PortWind Provider Sync

## Purpose
This capability documents the current PortWind synchronization workflow that keeps PortWind weather-station metadata and latest observations aligned with the worker's shared persistence model. It exists to refresh the PortWind station catalog on a slower maintenance cadence, apply PortWind-owned active-state rules to persisted weather stations, and then poll only active PortWind stations for latest observation data.

This capability starts in the worker layer at `IPortWindStationRefreshService.SyncWeatherStationsAsync(...)` and `IPortWindLatestDataSyncService.SyncLatestStationDataAsync(...)`. It flows through the PortWind client and mapping layer into the shared weather-station, station-data, and latest-station-data services. It includes the startup and recurring triggers, the JavaScript-wrapped station-catalog parsing contract, the readiness rules that determine whether a PortWind station is active, and the per-station failure handling used during latest-data polling.

It does not redefine worker-wide startup sequencing, recurring cron-loop behavior, or the generic weather-station and station-observation persistence contracts beyond the PortWind-specific behavior that those shared contracts support.

## Requirements

### Requirement: PortWind maintenance and observation sync run on startup and on separate recurring schedules
The worker SHALL invoke both PortWind workflows during startup and SHALL keep station-catalog maintenance on a different recurring cadence from latest-observation polling.

The current implemented recurring schedules are:
- `0 0 6 * * SUN` in UTC for `IPortWindStationRefreshService.SyncWeatherStationsAsync(...)`
- `0 3 * * * *` in UTC for `IPortWindLatestDataSyncService.SyncLatestStationDataAsync(...)`

#### Scenario: Startup refreshes PortWind stations before latest observations
- **GIVEN** the worker host has started and is executing startup jobs
- **WHEN** `StartupJobs.RunStartupJobsAsync(...)` reaches the PortWind steps
- **THEN** it resolves `IPortWindStationRefreshService` from a fresh scope and invokes `SyncWeatherStationsAsync(...)`
- **AND** after that step completes or fails it resolves `IPortWindLatestDataSyncService` from a fresh scope and invokes `SyncLatestStationDataAsync(...)`

#### Scenario: Recurring PortWind workflows stay on distinct maintenance and observation cadences
- **GIVEN** the worker has finished its startup-job phase
- **WHEN** `Worker.ExecuteAsync()` initializes recurring schedules
- **THEN** it starts a `CronScheduler<IPortWindStationRefreshService>` loop using `0 0 6 * * SUN`
- **AND** it starts a separate `CronScheduler<IPortWindLatestDataSyncService>` loop using `0 3 * * * *`

### Requirement: PortWind station refresh parses the published station catalog from the JavaScript payload
The PortWind station-refresh workflow SHALL fetch station metadata from the configured PortWind catalog endpoint, extract the `window.stations` assignment from the returned JavaScript payload, convert the extracted object literal into JSON-compatible content, and fail the refresh if the station catalog cannot be fully extracted or parsed.

For current implemented behavior, the client accepts JavaScript object-literal details including unquoted property identifiers, single-quoted strings, unicode escape sequences, and trailing commas, and it ignores unrelated script content outside the `window.stations` assignment.

#### Scenario: JavaScript-wrapped station data is extracted from the PortWind payload
- **GIVEN** the PortWind station catalog response contains a `window.stations = { ... }` assignment followed by additional script
- **WHEN** `FetchStationsAsync(...)` processes the response body
- **THEN** it extracts only the assigned object literal for `window.stations`
- **AND** it converts that object literal into JSON-compatible content before deserializing the station catalog

#### Scenario: Missing station-catalog assignment fails the refresh
- **GIVEN** the PortWind station catalog response does not contain a parseable `window.stations` assignment
- **WHEN** `FetchStationsAsync(...)` attempts to extract the station catalog
- **THEN** it throws an exception instead of returning a partial station set

### Requirement: PortWind station refresh maps provider readiness into provider-scoped weather-station state
The PortWind mapping and station-refresh workflow SHALL create weather stations only from catalog entries with valid non-zero geographic coordinates, SHALL normalize station labels before persistence, and SHALL treat a station as active only when PortWind marks both `status` and `history` as true.

For current implemented behavior:
- `Provider` is `PortWind`
- `Country` is seeded as `null`
- `IsMain` is seeded as `false`
- latitude and longitude are rounded to 5 decimals
- labels collapse repeated whitespace and attempt UTF-8 mojibake repair before persistence

#### Scenario: PortWind station metadata is normalized for weather-station upsert
- **GIVEN** a PortWind catalog entry has a station id, valid coordinates, and a station label
- **WHEN** `MapToStationRefreshResult(...)` maps that entry
- **THEN** it produces a `WeatherStation` keyed by the PortWind station id
- **AND** it normalizes the label and rounds coordinates before returning the mapped weather station

#### Scenario: PortWind readiness metadata controls whether a station is active
- **GIVEN** a PortWind catalog entry is otherwise valid for persistence
- **WHEN** the mapping layer evaluates the entry's provider readiness flags
- **THEN** the mapped weather station is active only when both `status` and `history` are `true`
- **AND** entries with missing or invalid coordinates are excluded from the mapped result

### Requirement: PortWind station refresh applies provider-authoritative lifecycle updates only within the PortWind scope
The PortWind station-refresh workflow SHALL upsert the mapped PortWind weather stations and then apply PortWind-specific active-state maintenance by activating stations seen as ready, inactivating stations explicitly seen as unavailable, and inactivating previously persisted PortWind stations that are missing from the current PortWind catalog.

These lifecycle updates SHALL only target weather stations whose provider is `PortWind`.

#### Scenario: Refresh applies PortWind active and inactive station buckets
- **GIVEN** the PortWind catalog has been mapped into seen, active, and inactive station-id buckets
- **WHEN** `SyncWeatherStationsAsync(...)` runs
- **THEN** it upserts the mapped weather stations through `IWeatherStationService`
- **AND** it activates only the PortWind station ids in the active bucket
- **AND** it explicitly inactivates only the PortWind station ids in the inactive bucket

#### Scenario: Missing PortWind stations are marked inactive without affecting other providers
- **GIVEN** previously persisted weather stations exist for PortWind and for other providers
- **AND** a previously persisted PortWind station is absent from the current PortWind catalog
- **WHEN** the refresh workflow applies missing-station maintenance
- **THEN** that missing PortWind station is marked inactive
- **AND** non-PortWind weather stations remain unchanged by the PortWind maintenance run

### Requirement: PortWind latest-data sync polls only active PortWind stations and persists mapped observations
The PortWind latest-data workflow SHALL fetch latest observations only for the currently active PortWind stations returned by `IWeatherStationService.GetActiveStationIdsByProviderAsync(...)`. For each polled station with a persistable latest observation, it SHALL upsert one historical `StationData` record and SHALL derive and upsert the corresponding `LatestStationData` projection from that same mapped observation.

For current implemented behavior, a PortWind observation is persistable only when `LastMeasurement`, `WindSpeedAverage`, and `WindDirectionAverage` are present. Wind gust uses `WindGust` when available and otherwise falls back to `WindSpeedMax`, and wind direction is rounded and normalized into the `0..359` range.

#### Scenario: A mapped PortWind latest observation populates both observation stores
- **GIVEN** an active PortWind station returns a latest payload with a last-measurement timestamp and usable wind data
- **WHEN** `SyncLatestStationDataAsync(...)` maps that payload
- **THEN** it upserts one `StationData` row for that station
- **AND** it converts that same mapped observation into `LatestStationData`
- **AND** it upserts the latest-station-data projection for that station

#### Scenario: A station without persistable latest data is skipped
- **GIVEN** an active PortWind station returns a latest payload without `LastMeasurement`, `WindSpeedAverage`, or `WindDirectionAverage`
- **WHEN** `MapToStationData(...)` evaluates that payload
- **THEN** it returns no `StationData` record for that station
- **AND** the latest-data sync does not persist either historical or latest-station-data for that station

### Requirement: PortWind latest-data polling isolates per-station failures
The PortWind latest-data workflow SHALL continue polling later active stations when one station fails. Invalid JSON payloads SHALL mark only the affected PortWind station inactive, while other station-level failures SHALL be logged without aborting the full PortWind polling run.

#### Scenario: One station fetch fails and later stations still run
- **GIVEN** multiple active PortWind stations are queued for latest-data polling
- **AND** one station's latest-data fetch throws a non-JSON exception
- **WHEN** `SyncLatestStationDataAsync(...)` processes the station list
- **THEN** it logs the error for that station
- **AND** it continues polling later active PortWind stations

#### Scenario: Invalid latest-data JSON marks only the affected station inactive
- **GIVEN** an active PortWind station returns a latest-data payload that throws `JsonException` during processing
- **WHEN** the latest-data sync catches that exception
- **THEN** it marks only that PortWind station inactive through `SetStationsInactiveByProviderAsync(...)`
- **AND** it continues processing the remaining active PortWind stations