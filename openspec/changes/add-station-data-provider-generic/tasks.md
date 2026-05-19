## 1. Provider Integration Setup

- [ ] 1.1 Confirm the selected Provider's authentication model, station metadata contract, observation contract, and batching limits.
- [ ] 1.2 Add the Provider integration types in WindLordApi.Integrations, including options validation, client abstraction, DTOs, and mapping services.
- [ ] 1.3 Register the Provider configuration and client dependencies in the worker startup path.

## 2. Station And Observation Workflows

- [ ] 2.1 Implement the Provider's WeatherStation sync path so metadata upserts happen before dependent observations are written.
- [ ] 2.2 Implement Provider observation mapping and ingestion through the existing StationData and LatestStationData services with provider-sized segmentation.
- [ ] 2.3 Wire the Provider into recurring orchestration through workflow-specific services rather than a combined all-purpose sync.

## 3. Verification

- [ ] 3.1 Add unit tests for Provider options validation, payload mapping, and workflow batching behavior.
- [ ] 3.2 Add integration coverage that proves WeatherStation metadata is persisted before Provider observations and latest-station snapshots still derive from observation history.
- [ ] 3.3 Validate the completed change with `openspec validate --specs`, targeted `dotnet test`, and `dotnet build` runs.