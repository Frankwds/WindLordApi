---
paths:
  - "src/**/*"
---

# Architecture

## Module Ownership
- `WindLordApi.Worker` owns cron schedules, startup jobs, health checks, and orchestration.
- `WindLordApi.Data` owns EF Core entities, repositories, services, and migrations.
- `WindLordApi.Integrations` owns outbound provider clients, DTOs, options, and mappings.
- `WindLordApi.Tests` owns test-only infrastructure.

## Preferred Data Flow
Provider client -> mapping -> worker service -> data service/repository -> PostgreSQL.

## Design Rules
- Keep provider concerns out of Data and Worker where possible.
- Keep persistence rules out of Integrations.
- Keep schedules, batching decisions, and startup workflows explicit in proposals and designs.
- Schema or data-shape changes require migration and rollback planning.

## OpenSpec
Use `openspec/specs/` as the source of truth for behavior and explain which module owns each part of a proposed change.