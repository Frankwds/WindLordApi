## Why

WindLordApi needs to support PortWind as a new weather station provider, but PortWind does not fit cleanly into the current MET-shaped weather station lifecycle seam. The provider publishes station metadata through a JavaScript payload rather than JSON, exposes provider-authoritative active state through `status` and `history`, and requires observation polling one station at a time.

Adding PortWind directly against the existing MET-specific repository and service methods would keep working code in the short term, but it would further hard-code provider behavior into the wrong layer. This change uses PortWind as the point to generalize station lifecycle operations to provider-scoped APIs while still preserving the current Worker -> Integrations -> Data boundaries.

## What Changes

- Add PortWind as a supported provider with its own integration client, DTOs, mappings, and worker orchestration.
- Generalize weather station lifecycle operations from MET-specific methods to provider-scoped repository and service methods, and update existing MET workflows to use the generalized seam where appropriate.
- Introduce a weekly PortWind station refresh that safely parses the `window.stations` JavaScript payload, upserts station metadata, applies provider-authoritative active state, and marks missing PortWind stations inactive.
- Introduce a separate PortWind latest-data sync that runs on startup and hourly, reads active PortWind station ids from the database, and persists normalized observations through the existing StationDataService and LatestStationDataService flows.
- Extend OpenSpec requirements and automated coverage for provider-scoped maintenance, PortWind-specific parsing and mapping, startup ordering, and reactivation or deactivation behavior.

## Capabilities

- Weekly PortWind station maintenance SHALL safely ingest the provider station catalog without executing remote JavaScript.
- PortWind weather stations SHALL derive active state from station-list membership plus `status` and `history`, defaulting to inactive when either field is missing or false.
- PortWind latest observation sync SHALL run independently from station maintenance and poll only active PortWind stations.
- Weather station lifecycle operations SHALL be available through provider-scoped data-layer APIs rather than MET-specific methods.
- PortWind station names SHALL be derived from cleaned `label` values only.

## Impact

- Affected modules: [openspec/specs/weather-station-integration/spec.md](/c:/Code/WindLordApi/openspec/specs/weather-station-integration/spec.md), [src/WindLordApi.Integrations](/c:/Code/WindLordApi/src/WindLordApi.Integrations), [src/WindLordApi.Worker](/c:/Code/WindLordApi/src/WindLordApi.Worker), [src/WindLordApi.Data](/c:/Code/WindLordApi/src/WindLordApi.Data), [src/WindLordApi.Tests](/c:/Code/WindLordApi/src/WindLordApi.Tests)
- Operational impact: add PortWind startup work, a weekly station refresh, and an hourly latest-data sync scheduled near the top of the hour without colliding with existing jobs.
- Data model impact: no initial schema or migration change is planned; PortWind will reuse the existing globally unique `station_id`, WeatherStation, StationData, and LatestStationData persistence model.
- Configuration impact: add PortWind configuration for base URLs and any schedule or timeout settings needed for deterministic startup and recurring syncs.