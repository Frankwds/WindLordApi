## 1. Spec Updates

- [x] 1.1 Update [openspec/specs/weather-station-integration/spec.md](/c:/Code/WindLordApi/openspec/specs/weather-station-integration/spec.md) with scenarios for provider-scoped station maintenance, provider-authoritative active state, and PortWind metadata-before-observation persistence.

## 2. Provider-Scoped Station Lifecycle

- [x] 2.1 Replace MET-specific weather station lifecycle methods in Data with provider-scoped query and update methods.
- [x] 2.2 Update existing MET workflows to use the generalized provider-scoped station lifecycle seam.
- [x] 2.3 Add repository and service tests covering provider-scoped activation, inactivation, and missing-station deactivation behavior.

## 3. PortWind Integration Layer

- [x] 3.1 Add PortWind options, client abstractions, DTOs, and safe station-catalog extraction in [src/WindLordApi.Integrations](/c:/Code/WindLordApi/src/WindLordApi.Integrations).
- [x] 3.2 Implement PortWind mappings for cleaned `label` names, default-inactive station state, `last_measurement` timestamps, and gust fallback from `wind_speed_max`.
- [x] 3.3 Add focused unit tests for station-catalog parsing, label cleanup, active-state derivation, and latest-data mapping edge cases.

## 4. PortWind Worker Orchestration

- [x] 4.1 Register PortWind services, configuration, startup jobs, and recurring schedules in [src/WindLordApi.Worker](/c:/Code/WindLordApi/src/WindLordApi.Worker).
- [x] 4.2 Implement a PortWind station refresh that upserts current stations, applies active and inactive state for seen stations, and marks missing PortWind stations inactive.
- [x] 4.3 Implement a separate PortWind latest-data sync that polls active PortWind stations hourly and continues after per-station failures.

## 5. Persistence And Validation

- [x] 5.1 Persist PortWind observations through StationDataService and LatestStationDataService without introducing PortWind-only schema changes.
- [x] 5.2 Add regression coverage for startup ordering, full-refresh parse failure behavior, reactivation and deactivation flows, empty latest-data responses, and per-station failure tolerance.
- [ ] 5.3 Validate the completed change with `openspec validate --specs`, `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`, and `dotnet build WindLordApi.sln`.