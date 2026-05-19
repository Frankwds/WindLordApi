## 1. PortWind Integration Layer

- [x] 1.1 Add the PortWind integration types in WindLordApi.Integrations, including options validation, client abstraction, raw station-list extraction, provider DTOs, and observation request support.
- [x] 1.2 Implement PortWind mapping for station metadata, mojibake label normalization, `data[].uts` timestamp conversion, `temperature_avg` normalization, and filtering of `*_previous` helper fields while ignoring provider-only metadata for persistence.
- [x] 1.3 Add focused unit tests for PortWind station-list parsing, trailing-JavaScript handling, label normalization, and observation mapping edge cases.

## 2. PortWind Worker Orchestration

- [x] 2.1 Register PortWind options, HTTP client, mapping service, PortWind Station Refresh, PortWind Observation Sync, and any related health-check wiring in the worker startup path.
- [x] 2.2 Implement PortWind Station Refresh so it parses the full station list, upserts current stations, marks missing PortWind stations inactive, and reactivates returning PortWind stations.
- [x] 2.3 Add provider-aware active PortWind station lookup, provider-sized observation batching, and split scheduling or startup-job wiring so PortWind Station Refresh runs weekly and PortWind Observation Sync polls active PortWind stations separately.

## 3. Persistence And Verification

- [x] 3.1 Integrate PortWind observations with the existing StationDataService and LatestStationDataService so latest snapshots continue to derive from persisted observation history.
- [x] 3.2 Add regression coverage that proves full-refresh parse failure behavior, reactivation and deactivation from station-list membership, per-station observation failure tolerance, empty observation responses, and non-persistence of PortWind comparative helper fields.
- [x] 3.3 Validate the completed change with `openspec validate --specs`, targeted `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`, and `dotnet build WindLordApi.sln`.