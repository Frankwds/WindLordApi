## Why

WindLordApi already supports multiple weather-station Providers, but PortWind introduces a station list contract and observation contract that do not fit the existing assumptions. A change proposal is needed so PortWind can be onboarded without weakening the existing guarantees around WeatherStation identity, metadata-before-observation persistence, and workflow-specific orchestration.

## What Changes

- Add PortWind as a supported Provider for WeatherStation metadata and station observation ingestion.
- Extend weather-station-network behavior so the worker can extract station metadata from PortWind's JavaScript station list payload, normalize station labels, upsert WeatherStation records by the existing globally unique station id model, and make station-list membership authoritative for PortWind activity.
- Extend observation-ingestion behavior so the worker fetches PortWind `latestandprevious` data only for active PortWind stations already in the database, maps `data[].uts` and representative measurement fields into normalized StationData rows, tolerates per-station request failures, and continues deriving LatestStationData from persisted observation history.
- Extend shared-sync-orchestration behavior so PortWind runs through validated options, registered client abstractions, and two workflow-specific jobs: a lower-frequency station refresh and a more frequent observation sync.
- Add implementation tasks for PortWind client parsing, observation batching, worker registration, configuration validation, and automated regression coverage.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `weather-station-network`: support PortWind station-list parsing, label normalization, globally unique station-id matching, and activity decisions driven by station-list membership.
- `observation-ingestion`: support PortWind per-station observation retrieval for active database stations, `uts`-based timestamps, `temperature_avg` normalization, partial-failure handling, and bounded ingestion of PortWind measurement data.
- `shared-sync-orchestration`: require explicit PortWind options validation, client registration, and isolated scheduling for PortWind Station Refresh and PortWind Observation Sync.

## Impact

Affected systems include WindLordApi.Integrations for a new PortWind client, options, DTOs, and mapping logic; WindLordApi.Worker for DI, schedules, startup jobs, and workflow orchestration; the existing data-layer services that persist WeatherStation, StationData, and LatestStationData records; and WindLordApi.Tests for provider-specific mapping and workflow regression coverage. The change adds another external Provider dependency but does not introduce a public API or an initial database schema change.