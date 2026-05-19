# WindLordApi Project Profile

## Tools
- GitHub Copilot
- Claude Code
- OpenAI Codex

## Project Overview
WindLordApi is a .NET 9 background worker solution that aggregates weather, wind, and geocoding data for paragliding locations. Scheduled sync jobs fetch external provider data, normalize it through mapping layers, and persist forecast and station information to PostgreSQL for downstream use.

## Structure & Stack
- Primary language: C# targeting .NET 9
- Solution layout: Worker host, Data library, Integrations library, xUnit test project
- Data access: Entity Framework Core 9 with Npgsql and FlexLabs upsert support
- Hosting model: .NET Worker Service with Microsoft.Extensions hosting and options binding
- Logging: Serilog with console and file sinks
- Scheduling: Cron-based background jobs with Cronos
- Testing: xUnit v3, FluentAssertions, Moq, Testcontainers for PostgreSQL
- Build and delivery: dotnet CLI, GitHub Actions, self-hosted Linux ARM64 deployment via rsync and systemd

## Architecture
- Style: modular layered worker service
- Layers:
  - Worker orchestrates cron schedules and startup work
  - Services coordinate sync workflows and domain operations
  - Integrations wrap external APIs and mapping logic
  - Data project owns EF Core models, repositories, and transaction boundaries
- Data flow:
  - External providers -> client and mapping layer -> sync services -> repositories and unit of work -> PostgreSQL
- External integrations:
  - MetYr for forecast updates
  - MetFrost for station metadata and observations
  - Holfuy for paragliding-specific station data
  - WindsMobi for wind station data
  - Google Geocoding for country enrichment

## Domain Overview
### Bounded Contexts
- Forecast supply for paragliding location forecasts
- Weather station network management for station metadata and active status
- Observation ingestion for latest and historical station data
- Location enrichment for country lookup and location metadata

### Core Entities
- ParaglidingLocation
- ForecastCache
- WeatherStation
- StationData
- LatestStationData

### Key Workflows
1. Forecast update batches locations, deletes stale forecast rows, fetches MetYr data, then upserts forecast cache entries.
2. MetFrost sync updates station metadata, station observations, and derived latest-station rows.
3. Holfuy and WindsMobi sync import provider-specific observations into normalized weather station data.
4. Country locator reverse geocodes paragliding locations to fill missing country information.

### Business Rules
- Forecast cleanup runs before fresh forecast ingestion.
- Active locations and active weather stations drive periodic sync selection.
- Weather station metadata is written before station observations for newly discovered stations.
- Latest station data is derived from station observations rather than written independently.
- Batch sizes are used to respect provider limits and database parameter constraints.

## Conventions
- Namespaces follow `WindLordApi.<Project>.<Feature>`.
- Types use PascalCase; interfaces use `I` prefixes; locals and parameters use camelCase.
- Integrations are organized per provider with `Client`, `Options`, `Mapping`, and model files.
- Data access follows repository and unit-of-work patterns.
- Configuration uses the options pattern with startup validation.
- Error handling favors argument validation, domain-specific invalid-operation failures, and logged HTTP exceptions.
- Tests are split into `Unit` and `Integration` folders and rely heavily on shared builders and fixtures.

## Quality & Delivery
- Primary validation commands:
  - `dotnet build WindLordApi.sln`
  - `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`
- CI/CD currently publishes and deploys on pushes to `main`, but does not visibly run tests in the deployment workflow.
- No dedicated linting or architecture enforcement configuration was found.

## Security & Infrastructure
- Secrets are expected in .NET user secrets rather than source-controlled settings.
- The worker uses provider API keys, OAuth credentials, and PostgreSQL connection strings.
- Deployment target is a self-hosted Linux ARM64 machine managed by systemd.
- PostgreSQL appears to be hosted through Supabase; outbound traffic may depend on a Fixie proxy.
- The codebase has no application-facing auth layer because it is a worker, but it handles sensitive integration credentials.

## Graph Signals
- Graphify output is metadata-heavy, but still highlights meaningful domain clusters around repository abstractions, service orchestration, MetYr forecasting, MetFrost sync, and Holfuy integration.
- Test builders appear as prominent hubs, indicating shared fixture patterns are central to the current test architecture.
- Forecast cache and station/location matching are likely hot paths worth preserving in specs and role guidance.

## Recommended OpenSpec Agent Set
- backend-developer
- tester
- architect
- project-manager
- database-expert
- security-engineer
- devops-engineer
- api-designer