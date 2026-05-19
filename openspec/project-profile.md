# Project Profile: WindLordApi

## Tools
- GitHub Copilot
- Claude Code
- OpenAI Codex

## Structure & Stack
- Language: C# with .NET 9 across Worker, Data, Integrations, and Tests.
- Package management and build: NuGet with `dotnet build`, `dotnet test`, `dotnet publish`, and `dotnet watch run`.
- Worker runtime: `Microsoft.NET.Sdk.Worker`, Cronos scheduling, Serilog logging, health checks.
- Persistence: EF Core 9 with Npgsql for PostgreSQL/Supabase and FlexLabs upsert support.
- Integrations: outbound HTTP clients for Holfuy, MetFrost, MetYr, WindsMobi, and Google Geocoding.
- Testing: xUnit v3, FluentAssertions, Moq, EF Core InMemory, and PostgreSQL Testcontainers.

## Architecture
- Style: layered background worker service with scheduled jobs and no public HTTP API surface.
- Module boundaries:
  - `src/WindLordApi.Worker`: hosted service, startup jobs, schedulers, health checks, orchestration.
  - `src/WindLordApi.Data`: EF Core models, repositories, services, unit-of-work, migrations.
  - `src/WindLordApi.Integrations`: provider clients, DTOs, mappings, options, DI extensions.
  - `src/WindLordApi.Tests`: unit and integration coverage for repositories and services.
- Primary data flow: provider client -> mapping -> worker sync/update service -> data service/repository -> PostgreSQL.
- Worker is the orchestration hub and owns cron scheduling for forecast refresh, provider sync, and country enrichment.

## Domain Overview
- Core bounded contexts:
  - Weather Station Integration: ingest and normalize provider stations and observations.
  - Forecast Cache: refresh and retain forecast data for paragliding locations.
  - Location Management: store paragliding location metadata and enrich weather-station country data.
- Core entities: `WeatherStation`, `StationData`, `LatestStationData`, `ParaglidingLocation`, `ForecastCache`.
- Domain vocabulary: provider, weather station, latest station data, forecast cache, paragliding location, main location, active station.
- Key workflows:
  - Scheduled provider syncs for Holfuy, MetFrost, and WindsMobi.
  - Forecast refresh in prioritized batches using missing-then-stale ordering.
  - Country lookup for stations missing country metadata.

## Quality & Standards
- Naming is standard .NET: PascalCase types/methods/properties, `I*` interfaces, `Async` suffix for async methods.
- Source is organized by layer and feature within each project rather than by end-to-end vertical slices.
- Public APIs and nontrivial classes frequently use XML documentation comments.
- Error handling uses explicit argument/state exceptions plus `ILogger<T>` logging in worker and integration paths.
- Tests are split into unit and integration folders and use realistic database coverage through Testcontainers.

## Security & Infrastructure
- Secrets are stored via ASP.NET Core user secrets and environment-backed configuration, not in tracked config files.
- Sensitive configuration includes Supabase/PostgreSQL connection strings and provider API keys.
- Deployment is via GitHub Actions to a self-hosted Linux ARM64 target with `dotnet publish`, `rsync`, and `systemctl restart`.
- Health checks cover database and upstream provider availability.

## Graph Note
- Graphify was run in AST-only mode because the installed CLI required an LLM API key for the default deep extraction path.
- The resulting graph artifacts were useful for confirming module boundaries and orchestration hot spots, but low-signal for fine-grained domain clustering.
- Domain partitioning therefore falls back to verified code structure and model/service analysis.