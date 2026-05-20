# CONTEXT.md

## Purpose
- WindLordApi is a .NET 9 background worker that synchronizes provider weather-station observations and forecast data for paragliding locations. There is no public HTTP API; risky edits usually affect schedules, persistence invariants, provider mappings, or startup behavior. `confirmed`
- Coverage of this pass: whole repo, with deeper local guidance for `src/WindLordApi.Data` and `src/WindLordApi.Worker`. `confirmed`

## Language
- Canonical terms are `Weather station`, `Station data`, `Latest station data`, `Forecast cache`, `Paragliding location`, and `Provider`. Match OpenSpec vocabulary when adding code, tests, or docs. `confirmed`
- `StationId` is the provider station identifier string. Entity `Id` values are database primary keys. Confusing those breaks joins, upserts, and sync contracts. `confirmed`
- `LocationId` in forecast code refers to the paragliding-location `Guid`, not an upstream provider code. `confirmed`

## System Boundary
- This repository is one worker inside a larger system. It has no UI or public API surface and appears to consume paragliding-location records managed elsewhere. `confirmed` for local code surface, `user-confirmed` for external ownership
- `all_paragliding_locations` is treated here as shared persisted metadata. The worker reads takeoff and landing coordinates, direction flags, `IsActive`, and `IsMain` for forecast selection, but no repo-owned create or update workflow for paragliding locations was found. `confirmed` for code paths, `strongly inferred` for shared ownership
- `IsMain` has the same business meaning across paragliding locations and weather stations, but different owners populate it: external location management for paragliding locations, provider mappings for some weather stations, and country enrichment for some unresolved stations. `user-confirmed` for meaning, `confirmed` for code paths

## Flagged Ambiguities
- Missing country is implemented as either `null` or `"UKJENT"` in the data layer. Treat both as missing in worker-owned country-enrichment behavior. `confirmed`
- `AGENTS.md` and `CLAUDE.md` lag current code in at least one place: PortWind is implemented and scheduled even though some top-level summaries still omit it. `confirmed`

## Architecture
- Behavioral source of truth is `openspec/specs/`; use the OpenSpec workflow when behavior changes. `confirmed`
- Safe flow is provider client -> mapping -> worker service -> data service or repository -> PostgreSQL. New code that jumps layers is usually wrong. `confirmed`
- `src/WindLordApi.Integrations` owns HTTP clients, DTOs, options, and mappings. `src/WindLordApi.Worker` owns startup jobs, schedules, health checks, and orchestration. `src/WindLordApi.Data` owns EF Core models, views, upsert keys, repositories, and transaction boundaries. `confirmed`

## Global Rules
- Provider weather-station metadata must exist before persisting dependent observations. Startup ordering and sync services assume that invariant. `confirmed`
- Station observations are unique by `(StationId, UpdatedAt)`. Forecast cache is unique by `(LocationId, Time)`. Latest station data is a separate read-optimized projection with one row per station. `confirmed`
- Forecast refresh consumes externally managed paragliding locations; this repo currently reads that metadata but does not own the location authoring lifecycle. `user-confirmed` for ownership, `confirmed` for code surface
- Forecast refresh deletes expired forecast rows before fetching or upserting new data, and work selection prefers locations with no forecasts before merely stale locations. `confirmed`
- Forecast refresh ultimately processes only active main paragliding locations because ID materialization filters by `IsActive && IsMain`. `confirmed`
- Country enrichment is best-effort: the worker retries stations whose country is `null` or `"UKJENT"`, persists successful resolutions, and leaves unresolved stations in place for later attempts. `confirmed`
- Schedules are code, not configuration: current cron expressions live in `src/WindLordApi.Worker/Worker.cs` and use six-field UTC Cronos expressions with seconds. `confirmed`
- Configuration chooses `SUPABASE_CONNECTION_STRING` outside production and `SUPABASE_CONNECTION_STRING_PRODUCTION` in production; `Program.cs` explicitly loads user secrets even outside Development. `confirmed`
- Unit tests can use EF InMemory for narrow helpers, but PostgreSQL behavior such as FlexLabs upsert, `ExecuteDelete`, views, filtered indexes, and transaction behavior should be validated with the PostgreSQL test container. `confirmed`
- Integration tests initialize schema with `EnsureCreatedAsync`, so migration-application behavior is not fully exercised by the normal test suite. Validate migrations separately when schema changes. `confirmed`

## Commands
- `openspec validate --specs`
- `dotnet build WindLordApi.sln`
- `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`

## Hot Spots
- `src/WindLordApi.Data/ApplicationDbContext.cs`: upsert keys, filtered indexes, keyless view mappings, and the `DateTimeKind.Unspecified` conversion for some PostgreSQL `timestamp without time zone` fields. `confirmed`
- `src/WindLordApi.Data/Repositories/WeatherStationRepository.cs`: weather-station upsert intentionally preserves existing `IsActive` except Holfuy inputs, and excludes `Country` and `IsMain` from normal upserts. `confirmed`
- `src/WindLordApi.Data/Repositories/ParaglidingLocationRepository.cs`: forecast candidate selection depends on database views plus an `IsActive && IsMain` filter when IDs are materialized. `confirmed`
- `src/WindLordApi.Worker/Services/CountryLocatorService.cs`: best-effort reverse geocoding, batching, retry shape, and `Country`/`IsMain` updates for unresolved weather stations. `confirmed`
- `src/WindLordApi.Worker/Startup/StartupJobs.cs`: startup ordering is operational behavior, especially PortWind station refresh before PortWind latest-data sync. `confirmed`
- `src/WindLordApi.Worker/Worker.cs`: schedule offsets appear intentionally staggered to reduce collisions and startup load. `strongly inferred`

## Local Guidance Map
- `src/WindLordApi.Data/CONTEXT.md`: PostgreSQL invariants, upsert semantics, and persistence watchouts
- `src/WindLordApi.Worker/CONTEXT.md`: startup order, cron runtime model, and operational behavior
- No local file for `src/WindLordApi.Integrations`: provider folders follow the repo-wide client, mapping, and options pattern closely enough that another layer would mostly paraphrase the tree. `strongly inferred`
- No local file for `src/WindLordApi.Tests`: the test-specific guidance that materially affects edit safety fits at the root without adding another vocabulary or runtime boundary. `strongly inferred`