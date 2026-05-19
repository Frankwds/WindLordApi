# WindLordApi

## Repository Expectations

- `openspec/specs/` is the source of truth for behavior.
- Use `/opsx:propose -> /opsx:apply -> /opsx:archive` for behavior-changing work.
- Validate with `openspec validate --specs`, `dotnet build WindLordApi.sln`, and targeted `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj` runs.

## Project Overview

WindLordApi is a .NET 9 background worker that aggregates forecast, weather-station, observation, and geocoding data for paragliding locations. It consumes MetYr, MetFrost, Holfuy, WindsMobi, and Google Geocoding, then persists normalized data to PostgreSQL through EF Core and repository abstractions.

## Tech Stack

- C# / .NET 9
- EF Core 9 + Npgsql + PostgreSQL
- Serilog
- Cron-based worker scheduling
- xUnit v3 + FluentAssertions + Moq + Testcontainers

## Architecture

- `src/WindLordApi.Worker`: host startup, schedulers, orchestration services, runtime wiring
- `src/WindLordApi.Integrations`: provider clients, options, mappings, provider models
- `src/WindLordApi.Data`: entities, repositories, data services, migrations, views, unit-of-work
- `src/WindLordApi.Tests`: unit and integration tests, helpers, builders

Keep forecast supply, weather-station maintenance, observation ingestion, and location enrichment as separate workflows unless OpenSpec explicitly changes that architecture.

## Domain Context

Use the existing domain terms consistently:
- `ParaglidingLocation`: tracked geographic site
- `WeatherStation`: provider-backed station metadata
- `ForecastCache`: persisted forecast rows for a location
- `StationData`: normalized historical observation rows
- `LatestStationData`: derived current observation snapshot
- `Provider`: upstream integration source

Important invariants:
- Forecast cleanup happens before refresh writes.
- Station metadata exists before dependent observation writes.
- Latest-station rows are derived from observation history.
- Batching is part of the behavior because provider and database limits shape execution.

## Coding Standards

- Follow `WindLordApi.<Project>.<Feature>` namespaces.
- Use PascalCase for files, types, and methods; `I` prefixes for interfaces; camelCase for locals and parameters.
- Keep provider-specific DTOs, auth, and mapping logic inside the relevant integration folder.
- Keep persistence inside data services, repositories, and unit-of-work abstractions.
- Use strongly typed options and startup validation for provider configuration.

## Security Rules

- Never hardcode provider secrets, connection strings, or tokens.
- Never log raw secret values or secret-bearing configuration.
- Validate external provider input before persisting shared models.
- Treat workflow, deployment, and appsettings changes as security-sensitive.

## Testing Conventions

- Use xUnit v3, FluentAssertions, Moq, and Testcontainers PostgreSQL.
- Mirror OpenSpec scenarios with tests.
- Reuse shared builders and helpers when building domain objects repeatedly.
- Prefer targeted `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj` while iterating.

## Role Guidance

### Backend Developer
- Implement worker services, integration workflows, and data-facing logic from the relevant OpenSpec spec.
- Preserve orchestration, mapping, and repository boundaries.

### Tester
- Translate OpenSpec scenarios into unit and integration coverage.
- Focus on batching, mapping, persistence, and regression risk.

### Architect
- Guard the modular worker architecture and cross-domain design decisions.
- Review schedule changes, schema changes, and provider additions carefully.

### Project Manager
- Drive changes through `/opsx:propose`.
- Keep scope, validation, and operational impact explicit.

### Database Expert
- Preserve entity integrity, migrations, views, uniqueness, and derived latest-data behavior.
- Document migration impact before implementation.

### Security Engineer
- Review secrets, workflow edits, provider auth changes, and logging behavior.
- Keep credentials out of source and logs.

### DevOps Engineer
- Maintain build, publish, rsync deployment, and systemd assumptions.
- Treat workflow edits as production-impacting.

### API Designer
- Keep provider contract shapes isolated inside integration folders.
- Use mapping abstractions to normalize external payloads into shared models.