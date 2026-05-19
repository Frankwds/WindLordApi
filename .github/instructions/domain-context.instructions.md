---
description: Project domain context, vocabulary, and behavioral invariants for WindLordApi
applyTo: "**/*"
---

# Domain Context

## Project Overview
WindLordApi is a .NET 9 background worker that synchronizes weather observations and forecasts for paragliding-related locations. Most changes affect provider integrations, scheduled workflows, persistence behavior, or operational reliability rather than request/response APIs.

## Domain Vocabulary

Use these terms consistently throughout the codebase:

| Term | Definition | Do NOT use |
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
Behavioral specifications live in `openspec/specs/`. Check the relevant spec before changing behavior, and update the spec via `/opsx:propose` if the current contract is missing or outdated.