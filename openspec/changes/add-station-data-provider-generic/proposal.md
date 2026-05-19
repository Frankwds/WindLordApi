## Why

The worker already supports multiple station-data providers, but adding another one currently relies on implicit integration patterns rather than an explicit change contract. A proposal is needed so a new provider can be introduced without weakening the existing guarantees around provider validation, weather-station identity, and metadata-before-observation persistence.

## What Changes

- Define the behavior required to onboard an additional station-data Provider into the existing worker architecture.
- Extend station-network requirements so a new provider's WeatherStation metadata is normalized and matched by provider identity before dependent data is written.
- Extend observation-ingestion requirements so station observations from the new provider are normalized and persisted through the existing data-layer boundaries.
- Extend shared sync orchestration requirements so the additional provider is registered, configured, and executed as its own workflow-specific service.
- Add implementation tasks covering provider client wiring, mapping, persistence integration, configuration, and regression tests.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `weather-station-network`: clarify how another provider can upsert WeatherStation metadata while preserving provider identity and activity semantics.
- `observation-ingestion`: define how another provider's station observations are normalized, deduplicated, and written through existing persistence services.
- `shared-sync-orchestration`: require the additional provider workflow to use validated configuration, registered clients, and isolated scheduling/orchestration.

## Impact

Affected systems include the integrations project for the new provider client and mapping logic, the worker project for DI and scheduling registration, the data-layer services already used by station metadata and observation workflows, and the test project for provider-specific regression coverage. The change does not introduce a public API, but it does expand provider configuration and recurring sync behavior.