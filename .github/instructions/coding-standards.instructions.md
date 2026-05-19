---
description: Coding standards for the Worker, Data, Integrations, and Tests projects
applyTo: "src/**/*"
---

# Coding Standards

## Naming Conventions

| Context | Convention | Example |
| --- | --- | --- |
| Files | PascalCase | `ForecastUpdateService.cs` |
| Classes | PascalCase | `WeatherStationService` |
| Interfaces | `I` + PascalCase | `IMetFrostClient` |
| Methods | PascalCase | `SyncLatestStationDataAsync` |
| Async methods | PascalCase + `Async` suffix | `LocateCountriesAsync` |
| Variables | camelCase | `forecastUpdateCron` |
| Database objects | existing snake_case naming in EF mappings | `forecast_cache_location_id_idx` |
| Environment/config keys | colon-delimited ASP.NET Core config keys | `ConnectionStrings:SUPABASE_CONNECTION_STRING` |

## File Organization
Code is organized by project and layer:

```text
src/
  WindLordApi.Worker/        orchestration, schedulers, startup, health checks
  WindLordApi.Data/          EF Core models, repositories, services, migrations
  WindLordApi.Integrations/  provider clients, DTOs, mappings, options
  WindLordApi.Tests/         unit and integration tests
```

## Imports
- Keep namespaces aligned with the owning project and feature area.
- Prefer direct references to the owning type or namespace rather than broad utility indirection.
- Do not introduce new cross-project dependencies lightly.

## Error Handling
- Validate arguments and state with descriptive exceptions.
- Use `ILogger<T>` for operational failures and preserve existing log semantics.
- Keep `CancellationToken` flow intact in async worker and integration code.

## Comments & Documentation
- Use comments for why a batching, retention, or schedule choice exists, not for obvious control flow.
- Match the existing XML documentation style for public contracts and nontrivial classes.

## Patterns to Follow
- Keep persistence logic in `WindLordApi.Data` services and repositories.
- Keep provider HTTP and DTO mapping logic in `WindLordApi.Integrations`.
- Keep scheduling, startup orchestration, and operational workflows in `WindLordApi.Worker`.

## Anti-Patterns to Avoid
- No ad hoc EF Core access from integration clients.
- No provider-specific DTOs leaking into persistence models outside mapping boundaries.
- No new public API/controller layer unless explicitly proposed.