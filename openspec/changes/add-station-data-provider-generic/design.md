## Context

WindLordApi already ingests station metadata and observations from multiple Providers through separate integration, worker, and data-layer responsibilities. Adding another station-data Provider should fit that existing shape instead of introducing a second orchestration pattern, because the current system already depends on provider-specific clients and mappings, validated configuration, metadata-before-observation writes, and repository-backed persistence.

The change is cross-cutting because it touches the integrations layer for the new Provider contract, the worker layer for service registration and scheduling, and the test suite for regression coverage. The design therefore needs to make the integration path explicit before implementation starts.

## Goals / Non-Goals

**Goals:**
- Add one more station-data Provider without changing the worker/data/integrations layering.
- Preserve the existing invariants for WeatherStation identity, metadata-before-observation persistence, and LatestStationData derivation.
- Require validated provider configuration, explicit client registration, and workflow-specific orchestration for the added Provider.
- Reuse existing persistence services and normalized models wherever the new Provider can map into the current schema.
- Add focused automated coverage for Provider mapping, orchestration, and persistence order.

**Non-Goals:**
- Redesign the entire station sync architecture into a plug-in framework.
- Introduce new public APIs or a new top-level worker responsibility.
- Expand the normalized persistence model for Provider-specific fields unless the selected Provider exposes data that cannot fit the current shared model.
- Solve cross-provider station deduplication beyond the existing provider identity plus provider station identifier boundary.

## Decisions

### Keep the Provider isolated inside the existing integrations pattern
The added Provider will live in its own integrations folder with a client abstraction, options type, provider DTOs, and mapping service that translate external payloads into shared models.

This keeps provider-specific concerns in WindLordApi.Integrations and preserves the current convention used by MetFrost, Holfuy, and WindsMobi.

Alternative considered: introduce a generic provider plug-in abstraction first. Rejected because the repository already has a workable provider pattern, and building a more abstract framework before a concrete need would add risk and delay.

### Keep station metadata and observation ingestion as separate workflow responsibilities
If the Provider supplies both station metadata and observations, the implementation will still preserve the boundary between weather-station maintenance and observation ingestion. Shared Provider code can be reused, but the worker should continue to execute metadata and observation concerns through workflow-specific services.

Alternative considered: combine the new Provider into one all-purpose sync service. Rejected because it would weaken the existing domain separation documented in the shared-sync-orchestration and weather-station-network specs.

### Reuse the existing normalized persistence services before considering schema changes
The new Provider should map into the current WeatherStation, StationData, and LatestStationData flows through the existing data services and unit-of-work boundary. Provider-specific fields that do not fit the shared model are out of scope for the initial change unless they are required to support the provider at all.

Alternative considered: add provider-specific tables or extend the shared schema up front. Rejected because the current proposal is to add another Provider, not to widen the domain model.

### Validate configuration at startup and register the Provider explicitly
The added Provider will use strongly typed options with startup validation and a registered client abstraction before any sync executes. Worker registration should make the added Provider visible as an explicit recurring responsibility rather than an implicit side effect of another service.

Alternative considered: lazy validation inside the first sync run. Rejected because configuration failures should surface at startup, consistent with the existing provider model.

### Prove the change with mapping, orchestration, and persistence-order tests
The implementation should add focused unit tests for provider mapping and workflow behavior, plus integration coverage for metadata-upsert ordering and normalized observation persistence where the current test suite can exercise those paths.

Alternative considered: rely on manual sync verification. Rejected because provider onboarding changes multiple workflows and carries regression risk across existing station-data behavior.

## Risks / Trade-offs

- Provider contract differs from existing Providers in timestamp, unit, or activity semantics -> Normalize only supported shared fields, validate assumptions in mapping code, and add tests for provider-specific edge cases.
- Provider rate limits or response shape require different batching behavior -> Keep provider-sized batching in the workflow and document any provider-specific segment sizing in implementation.
- Provider station identifiers are not globally unique -> Continue matching WeatherStation rows by provider identity plus provider station identifier.
- The Provider supplies metadata and observations through the same remote contract -> Share integration code where useful, but keep the worker workflow boundary separate so metadata and observations can still fail and retry independently.

## Migration Plan

Add the new Provider code and configuration binding, then deploy the worker with the required non-secret and secret configuration values in the same release. After deployment, run the Provider's first sync through the normal worker path so WeatherStation metadata is created before observation ingestion depends on it.

Rollback is a code-and-configuration rollback: remove or disable the Provider registration and configuration, then redeploy the previous worker version. No database migration is planned for the initial change.

## Open Questions

- Which specific Provider contract will be onboarded, and does it expose both WeatherStation metadata and station observations?
- Does the Provider publish station activity state directly, or will activity need to be inferred from other metadata?
- Does the Provider support batched observation fetches, or will the implementation need per-station calls with provider-sized throttling?