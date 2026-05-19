---
description: C# naming, organization, and implementation standards for WindLordApi source code
applyTo: "src/**/*"
---

# Coding Standards

## Naming Conventions

| Context | Convention | Example |
|---------|------------|---------|
| Files | PascalCase | `HolfuyClient.cs` |
| Namespaces | `WindLordApi.<Project>.<Feature>` | `WindLordApi.Integrations.MetYr` |
| Types | PascalCase | `ForecastUpdateService` |
| Interfaces | `I` + PascalCase | `IMetYrClient` |
| Methods | PascalCase | `SyncLatestStationDataAsync` |
| Locals and parameters | camelCase | `stationDataBatch` |
| Options types | `*Options` suffix | `MetFrostOptions` |
| Mapping types | `*Mapping` suffix | `HolfuyMapping` |
| Database tables and views | snake_case where persisted | `forecast_cache` |

## File Organization

Organize code by project and then by role inside that project.

```text
src/
  WindLordApi.Worker/       startup, schedulers, orchestration services
  WindLordApi.Data/         entities, repositories, data services, migrations
  WindLordApi.Integrations/ one folder per provider with client/options/mapping/models
  WindLordApi.Tests/        unit and integration tests plus helpers and builders
```

## Imports

- Prefer direct namespace imports over indirection-heavy helper layers.
- Keep provider-specific dependencies inside their integration folder.
- Do not couple worker orchestration directly to provider DTOs when a mapping abstraction exists.

## Error Handling

- Validate arguments explicitly at service boundaries.
- Use provider/client failures to surface actionable exceptions and logs.
- Keep persistence and transaction behavior inside the data layer or unit-of-work abstraction.

## Comments & Documentation

- Comment the reason for a non-obvious batch limit, scheduling rule, or provider quirk.
- Avoid comments that only restate code.
- Keep OpenSpec artifacts current when behavior changes.

## Patterns To Follow

- Use the options pattern with startup validation for provider configuration.
- Use repository and unit-of-work boundaries for persistence.
- Keep provider normalization inside mapping services.
- Keep worker services focused on orchestration, not raw persistence details.

## Anti-Patterns To Avoid

- No hardcoded secrets or connection strings.
- No direct `DbContext` access from orchestration code when repository services already own the behavior.
- No provider-specific DTO leakage into shared domain behavior.
- No silent schedule or batch-size changes without spec or proposal updates.