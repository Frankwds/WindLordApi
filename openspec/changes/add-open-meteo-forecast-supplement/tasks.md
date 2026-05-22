## 1. Spec Updates

- [ ] 1.1 Add forecast-refresh spec deltas for batched Open-Meteo supplementation, provider precedence, merge rules, WMO weather normalization, and per-location failure handling.
- [ ] 1.2 Add runtime-bootstrap spec deltas for Open-Meteo startup validation and advisory health-check coverage.

## 2. Open-Meteo Integration Layer

- [ ] 2.1 Add Open-Meteo options, client abstractions, DTOs, and batched forecast request construction in `src/WindLordApi.Integrations`.
- [ ] 2.2 Implement Open-Meteo mapping for hourly forecast rows, UTC timestamps, and WMO-to-Yr weather-code normalization.
- [ ] 2.3 Add focused unit tests for request building, batched response correlation, unsupported weather-code handling, and null precipitation min or max behavior.

## 3. Forecast Refresh Orchestration

- [ ] 3.1 Update `ForecastUpdateService` to start one batched Open-Meteo request for all selected takeoff locations while keeping MetYr fetches sequential per location.
- [ ] 3.2 Track the latest Yr timestamp per location, append only later Open-Meteo takeoff rows, and preserve Yr-only persistence when the Open-Meteo batch fails.
- [ ] 3.3 Skip persistence for locations whose Yr fetch fails even if Open-Meteo data exists, and keep landing forecast fields sourced only from Yr.

## 4. Startup Registration And Health

- [ ] 4.1 Register Open-Meteo options, client, mapping, and health check in `src/WindLordApi.Worker`.
- [ ] 4.2 Include Open-Meteo in the startup health-check pass without changing the current advisory startup behavior.

## 5. Validation

- [ ] 5.1 Add or update automated coverage for merge precedence, `IsYrData` semantics, response-to-location correlation, and startup health reporting.
- [ ] 5.2 Validate the completed change with `openspec validate --specs`, `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`, and `dotnet build WindLordApi.sln`.