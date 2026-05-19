---
paths:
  - "src/**/*"
---

# Coding Standards

## Naming
- Use PascalCase for files, classes, methods, and properties.
- Prefix interfaces with `I` and suffix asynchronous methods with `Async`.
- Keep database object names aligned with existing snake_case EF mappings.

## Organization
- `WindLordApi.Worker` owns orchestration, schedulers, startup jobs, and health checks.
- `WindLordApi.Data` owns entities, repositories, services, views, and migrations.
- `WindLordApi.Integrations` owns outbound clients, DTOs, options, and mappings.

## Error Handling
- Validate inputs and state explicitly with descriptive exceptions.
- Use `ILogger<T>` for operational failures and preserve existing log semantics.
- Preserve `CancellationToken` flow in async methods.

## Patterns to Follow
- Keep persistence logic inside `WindLordApi.Data`.
- Keep provider-specific parsing and DTO mapping inside `WindLordApi.Integrations`.
- Keep scheduling and startup orchestration inside `WindLordApi.Worker`.

## Anti-Patterns to Avoid
- No ad hoc EF Core access from integration clients.
- No provider DTO leakage into persistence models outside mapping boundaries.
- No new public API/controller layer unless it is explicitly proposed.