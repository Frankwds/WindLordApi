# WindLordApi

## Overview
WindLordApi is a .NET 9 background worker that aggregates forecast, station, observation, and geocoding data for paragliding locations. The source of truth for behavior is `openspec/specs/`, and all change work should follow `/opsx:propose -> /opsx:apply -> /opsx:archive`.

## Tech Stack
- C# / .NET 9
- EF Core 9 + Npgsql + PostgreSQL
- Serilog
- Cron-based worker scheduling
- xUnit v3 + FluentAssertions + Moq + Testcontainers

## Development Commands
- Build: `dotnet build WindLordApi.sln`
- Test: `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`
- OpenSpec validation: `openspec validate --specs`

## Architecture
- `src/WindLordApi.Worker`: host startup, scheduling, orchestration services
- `src/WindLordApi.Integrations`: provider-specific clients, options, mappings, models
- `src/WindLordApi.Data`: entities, repositories, data services, migrations, views
- `src/WindLordApi.Tests`: unit and integration tests plus builders and helpers

## Working Rules
- Read the relevant spec in `openspec/specs/` before editing behavior.
- Keep provider-specific DTOs and auth details inside the integration layer.
- Keep persistence behind data services, repositories, and the unit-of-work boundary.
- Treat workflow files, appsettings changes, and secrets-related work as high-review areas.

## Detailed Guidance
@.claude/rules/domain-context.md
@.claude/rules/coding-standards.md
@.claude/rules/security.md
@.claude/rules/testing.md
@.claude/rules/architecture.md
@.claude/rules/agent-backend-developer.md
@.claude/rules/agent-tester.md
@.claude/rules/agent-architect.md
@.claude/rules/agent-project-manager.md
@.claude/rules/agent-database-expert.md
@.claude/rules/agent-security-engineer.md
@.claude/rules/agent-devops-engineer.md
@.claude/rules/agent-api-designer.md