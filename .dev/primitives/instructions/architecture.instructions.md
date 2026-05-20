---
description: Architecture boundaries and module ownership for WindLordApi
applyTo: "src/**/*"
---

# Architecture

## Module Ownership
- `WindLordApi.Worker` owns scheduling, startup jobs, health checks, and orchestration.
- `WindLordApi.Data` owns EF Core entities, repositories, services, views, and migrations.
- `WindLordApi.Integrations` owns outbound clients, DTOs, provider options, and mappings.
- `WindLordApi.Tests` owns test-only infrastructure.

## Data Flow
Preferred flow is provider client -> mapping -> worker service -> data service/repository -> PostgreSQL.

## Change Design Rules
- Keep provider-specific parsing and DTO concerns out of Worker and Data.
- Keep persistence rules and constraints out of Integrations.
- Keep cron and startup orchestration out of Data and Integrations.
- New abstractions should extend existing options, mapping, repository, or service patterns before adding new layers.

## Operational Considerations
- Changes that affect schedules, batching, retention windows, or startup jobs must call that impact out in proposals and designs.
- Changes that affect schema or data shape must include migration and rollback planning.

## Specifications
Use `openspec/specs/` as the source of truth for behavioral boundaries. Architectural decisions should explain which module owns each part of the behavior.