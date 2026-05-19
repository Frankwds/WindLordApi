---
description: Architecture and layering rules for WindLordApi worker, data, and integration code
applyTo: "src/**/*"
---

# Architecture Rules

## Layer Boundaries

- `WindLordApi.Worker` owns host startup, scheduling, orchestration, and runtime wiring.
- `WindLordApi.Integrations` owns provider-specific clients, DTOs, options, and mapping abstractions.
- `WindLordApi.Data` owns entities, repositories, views, transaction boundaries, and data services.
- `WindLordApi.Tests` owns validation of those behaviors and should mirror the production boundaries.

## Domain Separation

- Keep forecast supply, weather-station maintenance, observation ingestion, and location enrichment as distinct workflows.
- Do not collapse provider-specific responsibilities into a generic catch-all sync path unless the spec and design explicitly change the architecture.

## Persistence

- Route writes through data services, repositories, and the unit-of-work boundary.
- Keep derived latest-station behavior distinct from historical station-data storage.
- Document schema or view changes before implementation.

## Integration Design

- Keep provider DTOs and option types local to the provider folder.
- Use mapping abstractions to normalize provider payloads into shared models.
- Prefer health checks and startup validation over runtime surprises for configuration issues.

## Operational Expectations

- Treat schedule changes, batching behavior, and deployment assumptions as architecture-level changes.
- Update `openspec/specs/` when observable workflow behavior changes.