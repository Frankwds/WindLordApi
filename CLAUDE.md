# WindLordApi

## Overview
WindLordApi is a .NET 9 background worker that synchronizes weather-station observations and forecast data for paragliding-related locations. It has no public HTTP API surface; the main runtime concerns are provider integrations, scheduled jobs, persistence, and operational reliability.

## Tech Stack
- C# / .NET 9
- Worker host with Cronos scheduling and Serilog logging
- EF Core 9 + Npgsql/PostgreSQL (Supabase)
- Provider integrations: Holfuy, MetFrost, MetYr, WindsMobi, Google Geocoding
- xUnit v3, FluentAssertions, Moq, Testcontainers

## Project Layout
- `src/WindLordApi.Worker`: schedulers, startup jobs, health checks, orchestration
- `src/WindLordApi.Data`: EF Core models, repositories, services, migrations
- `src/WindLordApi.Integrations`: provider clients, DTOs, mappings, DI registration
- `src/WindLordApi.Tests`: unit and integration tests
- `openspec/specs`: behavioral source of truth for seeded OpenSpec domains

## Development Commands
- Build: `dotnet build WindLordApi.sln`
- Test: `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`
- Publish: `dotnet publish WindLordApi.sln`
- Watch: `dotnet watch run --project WindLordApi.sln`
- OpenSpec validate: `openspec validate --specs`

## OpenSpec Workflow
- Treat `openspec/specs/` as the behavioral source of truth.
- Use `/opsx:propose` before changing behavior.
- Use `/opsx:apply` to implement approved changes.
- Use `/opsx:archive` after validation is complete.

## Role Guidance
### Backend Developer
Focus on worker orchestration, integration clients, data services, and repository-backed behavior. Preserve the existing Worker -> Integrations/Data layering.

### Tester
Map OpenSpec scenarios to xUnit tests. Prefer integration tests for persistence or multi-layer workflows and unit tests for mappings, services, and schedulers.

### Architect
Protect module boundaries, cron-driven workflows, and persistence ownership. New abstractions should follow existing options, mapping, repository, and service patterns.

### Database Expert
Own EF Core model changes, migrations, indexes, and invariants such as unique station observations and forecast retention behavior.

### Security Engineer
Protect user-secrets-based configuration, provider credentials, and database connection strings. Review new external integrations and logging of sensitive values.

### DevOps Engineer
Own GitHub Actions deployment, publish outputs, and systemd operational expectations on the Linux host.

### Project Manager
Ensure changes identify affected domains, modules, schedules, migrations, and validation commands before implementation begins.

## Detailed Rules
@.claude/rules/domain-context.md
@.claude/rules/coding-standards.md
@.claude/rules/security.md
@.claude/rules/testing.md
@.claude/rules/architecture.md