# WindLordApi

## Project Overview
WindLordApi is a .NET 9 scheduled worker that ingests weather-station observations and forecast data for paragliding-related locations. It does not expose a public HTTP API; the important behavior lives in provider integrations, cron-driven jobs, persistence workflows, and operational health checks.

## Repository Expectations
- Read `openspec/specs/` before changing behavior.
- Use root and local `CONTEXT.md` files for current repo-state facts, hotspots, and operational caveats that are too volatile for stable rule files.
- Use the `/opsx:propose -> /opsx:apply -> /opsx:archive` workflow for behavior changes.
- Validate behavior with focused commands such as `openspec validate --specs`, `dotnet build WindLordApi.sln`, and `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`.
- Do not commit secrets or provider credentials; use user secrets and environment-backed configuration.

## Tech Stack
- C# / .NET 9
- Worker host with Cronos scheduling and Serilog
- EF Core 9 with Npgsql/PostgreSQL and FlexLabs upsert
- Integrations: Holfuy, MetFrost, MetYr, PortWind, WindsMobi, Google Geocoding
- xUnit v3, FluentAssertions, Moq, Testcontainers

## Architecture
- `src/WindLordApi.Worker` orchestrates cron schedules, startup jobs, health checks, and provider workflows.
- `src/WindLordApi.Data` owns EF Core models, repositories, services, migrations, and persistence invariants.
- `src/WindLordApi.Integrations` owns outbound clients, DTOs, mappings, options, and DI helpers.
- `src/WindLordApi.Tests` covers repository and service behavior with unit and integration tests.
- Preserve the direction of data flow: provider client -> mapping -> worker service -> data service/repository -> PostgreSQL.

## Domain Context
### Project Vocabulary
Use these terms consistently:

| Term | Meaning | Avoid |
| --- | --- | --- |
| Weather station | Persisted external station metadata identified by provider station id | Sensor, device record |
| Station data | Historical point-in-time observation for a station | Reading cache |
| Latest station data | Read-optimized latest observation for a station | Snapshot history |
| Forecast cache | Persisted forecast entries for a paragliding location | Live forecast stream |
| Paragliding location | Named flight-related location with directional metadata | Generic place |
| Provider | External data source such as Holfuy or MetFrost | Vendor adapter |

### Business Rules
- Station observations MUST remain unique by station and timestamp.
- Latest station data MUST reflect the newest persisted observation for a station.
- Forecast refresh MUST remove expired forecasts before storing new ones.
- Locations without forecast coverage SHOULD be prioritized before stale locations.
- Country enrichment SHOULD target stations that have coordinates but no country metadata.

## Coding Standards
### Naming and Organization
- Use standard .NET naming: PascalCase for types/methods/properties, `I*` interfaces, `Async` suffixes for async methods.
- Keep file and namespace organization aligned to the existing layer structure inside each project.
- Do not bypass `WindLordApi.Data` services and repositories with ad hoc persistence logic in Worker or Integrations.

### Error Handling
- Validate arguments and state explicitly with descriptive exceptions.
- Log operational failures with `ILogger<T>` before swallowing or rethrowing when the surrounding pattern already does so.
- Preserve cancellation token flow in asynchronous worker and integration code.

### Documentation
- Match the existing use of XML comments on public contracts and nontrivial classes.
- Comment the reason for unusual batching, retention, or scheduling behavior rather than narrating obvious code.

## Security Rules
- Secrets belong in user secrets, environment variables, or deployment configuration, never in tracked JSON files or code.
- Treat Supabase/PostgreSQL connection strings and provider API keys as sensitive.
- Do not log secrets, raw credentials, or full provider auth headers.
- Review new outbound integrations for configuration, retry behavior, and secret handling before implementation.

## Testing Requirements
- Use xUnit v3 for both unit and integration coverage.
- Prefer integration tests for repository, migration, or multi-layer workflows that depend on PostgreSQL behavior.
- Prefer unit tests for mappings, pure services, and scheduler logic.
- Map OpenSpec `Given/When/Then` scenarios to concrete tests when behavior changes.

## Role Guidance
### Backend Developer
Own worker orchestration, provider clients, data services, and persistence-backed business logic. Keep Worker as the orchestrator and preserve mapping/service/repository boundaries.

### Tester
Translate OpenSpec scenarios into xUnit tests. Cover repository/data-service workflows with Testcontainers when behavior depends on PostgreSQL semantics.

### Architect
Protect the layered worker architecture and existing separation between Worker, Data, and Integrations. New patterns should extend the existing options, mapping, repository, and service conventions.

### Project Manager
Require proposals to identify affected modules, domains, schedules, migrations, and validation commands. Keep tasks small and dependency-ordered.

### Database Expert
Own EF Core model changes, migrations, constraints, indexes, and retention/query behavior. Schema changes need explicit migration and rollback planning.

### Security Engineer
Review secret management, provider credentials, connection strings, and new external integrations. Security concerns block implementation until resolved.

### DevOps Engineer
Own GitHub Actions deployment flow, publish outputs, and operational expectations for the self-hosted Linux systemd deployment target.