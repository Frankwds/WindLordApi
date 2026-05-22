## 1. Spec Updates

- [ ] 1.1 Add forecast-refresh spec deltas for the split MetYr and Open-Meteo workflows, provider-specific failure behavior, and Open-Meteo-specific location selection.
- [ ] 1.2 Add forecast-cache lifecycle spec deltas for persistence-enforced Yr precedence on conflicting `(LocationId, Time)` upserts.
- [ ] 1.3 Add worker-schedule spec deltas for separate MetYr and Open-Meteo recurring cadences.

## 2. Worker Service Split

- [ ] 2.1 Split the current forecast refresh orchestration into separately named worker services for authoritative MetYr refresh and Open-Meteo supplementation.
- [ ] 2.2 Update worker DI registration and recurring scheduler wiring so MetYr runs every 5 minutes, Open-Meteo runs every 10 minutes, and cleanup ownership remains with the MetYr workflow.

## 3. Persistence Precedence

- [ ] 3.1 Update the forecast-cache repository upsert contract so incoming Open-Meteo rows cannot overwrite existing Yr-backed rows for the same `(LocationId, Time)`.
- [ ] 3.2 Ensure later Yr-backed rows can replace existing Open-Meteo-backed rows for the same `(LocationId, Time)` key.

## 4. Open-Meteo Selection And Supplement Flow

- [ ] 4.1 Add an Open-Meteo-specific location-selection path that prioritizes locations with no Open-Meteo-backed rows first and then locations whose Open-Meteo-backed rows were updated longest ago.
- [ ] 4.2 Keep the Open-Meteo workflow takeoff-only, batch-oriented, and independently failure-isolated from the authoritative MetYr workflow.

## 5. Validation

- [ ] 5.1 Add repository integration coverage for `Yr over Open-Meteo` and `Yr replaces Open-Meteo` conflict cases.
- [ ] 5.2 Add service-level coverage for split scheduling ownership and Open-Meteo-specific selection behavior.
- [ ] 5.3 Validate the completed change with `openspec validate --specs`, `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`, and `dotnet build WindLordApi.sln`.