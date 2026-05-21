# Holfuy Provider Sync

## Purpose
This capability documents the current Holfuy observation sync workflow that ingests live Holfuy station measurements into the worker's weather-station persistence model. It exists to keep Holfuy weather stations present in the database, persist each observation as historical station data, and refresh the read-optimized latest-station-data projection from the same fetched payload.

This capability starts at `IHolfuySyncService.SyncHolfuyDataAsync(...)` in the worker layer and flows through the Holfuy HTTP client and mapping layer into the shared weather-station, station-data, and latest-station-data persistence services. It includes the startup and scheduled triggers that invoke the sync, the configuration contract required to reach Holfuy, the filtering and mapping rules applied to Holfuy payloads, and the persistence ordering that keeps dependent station data tied to an existing weather station.

It does not define the worker-wide startup failure-isolation contract, recurring cron loop behavior, or generic weather-station lifecycle rules beyond the Holfuy-specific effects that this workflow relies on.

## Requirements

### Requirement: Holfuy sync runs on startup and on the Holfuy cron schedule
The worker SHALL invoke the Holfuy sync once during startup and SHALL schedule recurring Holfuy sync runs on the declared Holfuy cron expression.

The current implemented recurring schedule is `30 */15 * * * *` in UTC, which runs every 15 minutes at second 30.

#### Scenario: Startup includes an immediate Holfuy sync
- **GIVEN** the worker host has started and is executing startup jobs
- **WHEN** `StartupJobs.RunStartupJobsAsync(...)` reaches the Holfuy step
- **THEN** it resolves `IHolfuySyncService` from a fresh scope
- **AND** it invokes `SyncHolfuyDataAsync(...)` before the startup sequence completes

#### Scenario: Recurring Holfuy sync uses the declared cron schedule
- **GIVEN** the worker has finished its startup-job phase
- **WHEN** it initializes recurring schedules in `Worker.ExecuteAsync()`
- **THEN** it starts a `CronScheduler<IHolfuySyncService>` loop for Holfuy
- **AND** that loop uses the cron expression `30 */15 * * * *`

### Requirement: Holfuy connectivity depends on configured credentials and optional local proxying
The Holfuy client SHALL require a configured Holfuy API key before attempting a fetch.

When `IS_LOCAL` is enabled, the Holfuy client SHALL require a configured `FIXIE_URL` proxy connection string and SHALL use that proxy for outbound Holfuy requests.

#### Scenario: Missing Holfuy API key prevents the fetch
- **GIVEN** the Holfuy client is invoked without a configured `Holfuy:ApiKey`
- **WHEN** `FetchHolfuyDataAsync(...)` begins
- **THEN** it logs that the API key is not configured
- **AND** it throws `InvalidOperationException`

#### Scenario: Local execution requires a Fixie proxy
- **GIVEN** configuration sets `IS_LOCAL` to `true`
- **WHEN** the application registers the Holfuy client
- **THEN** it requires `ConnectionStrings:FIXIE_URL` to include proxy host, port, and credentials
- **AND** it configures the Holfuy `HttpClient` to send requests through that proxy

### Requirement: Holfuy fetches the full live measurement feed and rejects stations with invalid coordinates
The Holfuy client SHALL request the full live measurement feed from Holfuy and SHALL exclude stations whose coordinates cannot be parsed or fall outside valid non-zero latitude and longitude ranges.

The current request shape is the Holfuy live endpoint with the parameters `s=all`, `m=JSON`, `tu=C`, `su=m/s`, `avg=1`, `utc`, and `loc`, plus the configured API key.

#### Scenario: Stations with invalid coordinates are excluded from the result
- **GIVEN** the Holfuy API returns one or more measurements whose latitude or longitude is missing, unparsable, zero, or outside valid geographic bounds
- **WHEN** the Holfuy client processes the response
- **THEN** those measurements are omitted from the mapped weather-station and station-data results
- **AND** only measurements with valid non-zero coordinates continue into persistence mapping

#### Scenario: Non-success or invalid JSON responses fail the fetch
- **GIVEN** the Holfuy API returns a non-success HTTP status or a response that cannot be deserialized into `HolfuyResponse`
- **WHEN** `FetchHolfuyDataAsync(...)` handles the response
- **THEN** it logs the failure details
- **AND** it throws an exception instead of returning partial mapped data

### Requirement: Holfuy mapping normalizes observations into station and measurement records
The Holfuy mapping layer SHALL convert each eligible Holfuy measurement into one `WeatherStation` record and one `StationData` record keyed by the Holfuy station identifier.

For current implemented behavior:
- `StationId` is the Holfuy `stationId` converted to string
- `Provider` is `Holfuy`
- `IsActive` is seeded as `true`
- `Country` is seeded as `Norway`
- `IsMain` is seeded as `true`
- latitude and longitude are rounded to 5 decimals for `WeatherStation`
- wind direction is rounded and normalized into the `0..359` range for `StationData`
- invalid `dateTime` values fall back to the current UTC time when creating `StationData`

#### Scenario: Holfuy measurements are mapped into the shared domain model
- **GIVEN** the Holfuy client has received a valid measurement with valid coordinates
- **WHEN** the mapping layer transforms that measurement
- **THEN** it creates a `WeatherStation` using the Holfuy station metadata
- **AND** it creates a `StationData` record using the same station identifier and the measurement's wind and temperature values

#### Scenario: Wind direction is normalized before persistence
- **GIVEN** a Holfuy measurement contains a wind direction outside the persisted direction range
- **WHEN** the mapping layer creates `StationData`
- **THEN** it rounds the direction
- **AND** it normalizes the value into the persisted `0..359` range before returning the mapped record

### Requirement: Holfuy sync persists weather stations before dependent observations
The Holfuy sync workflow SHALL upsert weather stations before it attempts to persist station observations so that dependent station data references an existing weather station key.

#### Scenario: Newly discovered Holfuy stations are persisted before their measurements
- **GIVEN** the Holfuy payload contains a station that is not yet present in the database
- **WHEN** `SyncHolfuyDataAsync(...)` processes that payload
- **THEN** it first upserts the mapped `WeatherStation` records through `IWeatherStationService`
- **AND** only after that does it upsert the mapped `StationData` records through `IStationDataService`

#### Scenario: Empty weather-station results do not block later station-data handling
- **GIVEN** the Holfuy client returns no mapped weather stations
- **WHEN** the sync workflow reaches the weather-station persistence step
- **THEN** it logs a warning that there are no weather stations to upsert
- **AND** it still continues to evaluate whether there is station data to persist

### Requirement: Holfuy sync keeps seen Holfuy stations active and persists observations idempotently
The Holfuy sync workflow SHALL keep Holfuy stations active when they are seen in Holfuy sync input, SHALL persist historical station observations using the shared `(StationId, UpdatedAt)` uniqueness contract, and SHALL refresh the latest-station-data projection from the same mapped observations.

For current implemented behavior:
- weather-station upsert matches on `StationId`
- when a matched weather station's incoming provider is `Holfuy`, the upsert forces `IsActive = true`
- matched weather-station upserts do not update `Country` or `IsMain`
- station-data upsert matches on `(StationId, UpdatedAt)` and does not update existing rows
- latest-station-data upsert matches on `StationId` and overwrites the projection with the latest mapped values

#### Scenario: Seen Holfuy stations remain active on upsert
- **GIVEN** a Holfuy weather station already exists in the database
- **WHEN** Holfuy sync upserts that station again
- **THEN** the upsert preserves shared reserved fields such as `Country` and `IsMain`
- **AND** it forces `IsActive` to `true` for that Holfuy station

#### Scenario: Historical station data remains unique by station and timestamp
- **GIVEN** Holfuy sync attempts to persist a station observation whose `StationId` and `UpdatedAt` already exist in `StationData`
- **WHEN** the station-data upsert runs
- **THEN** it does not overwrite the existing historical record
- **AND** the workflow continues using the shared idempotent insert behavior

#### Scenario: Latest-station-data is derived from the same Holfuy observation batch
- **GIVEN** Holfuy sync has persisted or attempted to persist a batch of mapped `StationData`
- **WHEN** the workflow builds the latest-data projection
- **THEN** it converts the same mapped station-data batch into `LatestStationData`
- **AND** it upserts one latest-data row per `StationId`

### Requirement: Holfuy sync logs and rethrows workflow failures
The Holfuy sync service SHALL log a sync failure and rethrow the exception to its caller when the Holfuy workflow fails.

#### Scenario: Holfuy sync does not swallow workflow exceptions
- **GIVEN** the Holfuy client or a downstream persistence step throws during `SyncHolfuyDataAsync(...)`
- **WHEN** the sync service catches that exception
- **THEN** it logs `Holfuy: Error syncing data`
- **AND** it rethrows the exception so the caller can apply startup or scheduler-level failure handling