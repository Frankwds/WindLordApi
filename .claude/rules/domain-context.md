# Domain Context

## Project Overview
WindLordApi is a .NET 9 background worker that synchronizes weather observations and forecasts for paragliding-related locations. Most changes affect provider integrations, scheduled workflows, persistence behavior, or operational reliability rather than request/response APIs.

## Domain Vocabulary

| Term | Definition | Avoid |
| --- | --- | --- |
| Weather station | Persisted external station metadata identified by a provider station id | Sensor, device record |
| Station data | Historical point-in-time observation for a station | Snapshot cache |
| Latest station data | Read-optimized latest observation per station | History table |
| Forecast cache | Persisted forecast entries for a paragliding location | Live forecast stream |
| Paragliding location | Flight-related location with directional suitability metadata | Generic place |
| Provider | External upstream data source such as Holfuy or MetFrost | Vendor adapter |

## Business Rules
- Station observations MUST remain unique by station and timestamp.
- Latest station data MUST reflect the newest persisted observation for a station.
- Forecast refresh MUST delete expired forecast data before writing new data.
- Locations without forecast coverage SHOULD be prioritized before merely stale locations.
- Stations missing country metadata SHOULD be candidates for scheduled enrichment.

## Specifications
Read `openspec/specs/` before changing behavior. If the contract is missing or stale, update it through the OpenSpec workflow first.