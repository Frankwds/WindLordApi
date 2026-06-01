# CONTEXT.md

## Scope
- Applies to host startup, cron scheduling, health-check orchestration, and background workflow composition. `confirmed`

## Language
- A `startup job` runs once during service start before cron loops begin. A `scheduled job` runs repeatedly through `CronScheduler<TService>`. Do not treat them as interchangeable. `confirmed`
- `Sync` methods here orchestrate provider fetch, mapping, and data-layer persistence; persistence rules still live in `WindLordApi.Data`. `confirmed`

## Local Intent
- This layer sequences and schedules workflows without absorbing provider DTO logic or persistence rules. `confirmed`
- Changes here are risky when they alter ordering, cadence, or failure isolation rather than business calculations. `confirmed`

## Structure
- `Program.cs` wires DI, logging, connection-string loading, Local-environment overrides, and startup health checks including schema-contract validation. `confirmed`
- `Startup/StartupJobs.cs` is the one-time boot sequence. `Worker.cs` defines hard-coded cron cadences and launches long-running scheduler loops. `confirmed`
- `Schedulers/CronScheduler.cs` parses six-part cron expressions with seconds support and resolves a fresh scoped service for each run. `confirmed`

## Local Rules
- Startup order matters: PortWind station refresh runs before PortWind latest-data sync so provider station metadata exists before dependent observations. `confirmed`
- Forecast startup order also matters: Open-Meteo supplement runs before the authoritative MetYr refresh, but MetYr still owns cleanup and remains the authoritative writer on overlapping forecast-cache rows. `confirmed`
- Startup job failures are logged and isolated; one failure does not stop later startup jobs. `confirmed`
- Scheduled-job failures are logged inside `CronScheduler` and the loop continues on the next occurrence. `confirmed`
- All cron expressions are UTC and currently hard-coded in `Worker.cs`; changing cadence requires code changes and should be reviewed against the existing staggered schedule. `confirmed`
- Open-Meteo currently runs behind an external free-tier request limit of 10,000 requests per day. Treat provider quota exhaustion as an operational expectation that degrades forecast coverage back toward Yr-only rows rather than as a behavioral guarantee. `user-confirmed`
- Current staggered cadence is:
  - WindsMobi every 5 minutes at second 0. `confirmed`
- Authoritative MetYr refresh every 5 minutes at minute offset 1. `confirmed`
- Open-Meteo supplement every 10 minutes at minute offset 6. `confirmed`
  - MetFrost latest data every 5 minutes at minute offset 2. `confirmed`
  - PortWind latest data hourly at minute 3. `confirmed`
  - Holfuy every 15 minutes at second 30. `confirmed`
  - Sunday maintenance window: MetFrost station discovery at 03:00 UTC, MetFrost active-status sync at 04:00 UTC, country locator at 05:00 UTC, PortWind station refresh at 06:00 UTC. `confirmed`
- `Program.cs` no longer checks EF migration history at startup; it relies on startup health checks, including `forecast-cache-schema`, to verify the live database contract. `confirmed`
- `Program.cs` explicitly loads user secrets even outside Development to support production debugging. Treat that as an operational choice, not a default host assumption. `confirmed`

## Validation
- Use `dotnet build WindLordApi.sln` for worker wiring or schedule changes. `confirmed`
- Use `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj` for service behavior changes, especially forecast and country-locator orchestration. `confirmed`
- Behavior changes that alter cadence, startup ordering, or workflow coverage should be checked against `openspec/specs` first. `confirmed`

## Watchouts
- The worker prints expected durations and next-run times for all jobs on startup. Schedule edits should keep the overview meaningful and avoid accidental collisions. `confirmed`
- `CronScheduler` creates a DI scope per execution. Avoid caching scoped services across runs or moving state into the scheduler itself. `confirmed`
- The host has no controller surface; follow the existing logging and startup health-check pattern instead of introducing an HTTP API by default. `strongly inferred`