## Why

WindLordApi currently treats forecast refresh as one combined workflow inside `ForecastUpdateService`. That was workable while Open-Meteo was an in-process supplement to the same per-location write path, but it couples two providers that now have materially different operational needs.

MetYr is the higher-quality source and should remain the authoritative provider for overlapping timestamps. It is also the provider that supports the near-term refresh behavior WindLord benefits from most, so it should continue to run frequently. Open-Meteo is valuable mainly as a longer-horizon supplement, but its free-tier request quota is operationally constrained and does not justify the same refresh cadence. Running both providers inside one shared five-minute workflow forces a single schedule and leaves provider precedence protected only by the current in-memory merge shape.

This change separates the two forecast responsibilities into explicitly named provider-owned worker services with different cron cadences, while turning "Yr always wins" into an explicit persistence rule rather than an incidental side effect of the current orchestration. The intent is to keep near-term Yr data fresh every five minutes, refresh Open-Meteo less frequently, and ensure Open-Meteo never overwrites higher-quality Yr data when both providers touch the same `(LocationId, Time)` row.

## What Changes

- Split the current forecast refresh orchestration into two separately named worker services: one for authoritative MetYr forecast refresh and one for Open-Meteo forecast supplementation.
- Schedule the MetYr refresh service every 5 minutes and the Open-Meteo supplement service every 10 minutes in `src/WindLordApi.Worker/Worker.cs`.
- Keep MetYr responsible for the authoritative near-term forecast write path, expired-row cleanup, and landing forecast enrichment where landing coordinates exist.
- Make Open-Meteo responsible only for lower-frequency takeoff forecast supplementation and never for landing forecast fields.
- Preserve the existing forecast-cache uniqueness contract on `(LocationId, Time)` while changing the upsert rules so Open-Meteo rows can never overwrite Yr-backed rows for overlapping timestamps.
- Ensure a later Yr refresh can replace or supersede an overlapping Open-Meteo row for the same `(LocationId, Time)` key.
- Revisit location selection so the Open-Meteo supplement path prioritizes locations with the shortest remaining Open-Meteo forecast tail, represented operationally by locations without Open-Meteo supplementation first and then locations whose Open-Meteo rows were updated longest ago.
- Update worker schedule documentation, forecast refresh requirements, and automated coverage to reflect the provider split, separate cadence, and precedence rules.

## Capabilities

- Forecast refresh SHALL expose separate recurring worker workflows for MetYr refresh and Open-Meteo supplementation.
- The MetYr refresh workflow SHALL run every 5 minutes, SHALL own expired-row cleanup, and SHALL remain the authoritative source for overlapping forecast timestamps.
- The Open-Meteo supplement workflow SHALL run every 10 minutes and SHALL supplement takeoff forecast coverage without taking ownership of the near-term authoritative write path.
- For any overlapping `(LocationId, Time)` forecast-cache row, Open-Meteo SHALL NOT overwrite a Yr-backed row, and that rule SHALL be enforced by the forecast-cache persistence contract rather than relying only on worker-side filtering.
- For any overlapping `(LocationId, Time)` forecast-cache row, a later Yr write SHALL be allowed to replace or supersede an Open-Meteo-backed row.
- Open-Meteo SHALL remain limited to takeoff forecast supplementation and SHALL NOT populate landing forecast fields.
- When Open-Meteo quota exhaustion or other batch failures occur, the MetYr refresh workflow SHALL continue to persist Yr-derived rows on its normal cadence.
- The system SHOULD be able to prioritize MetYr freshness and Open-Meteo horizon supplementation independently when selecting locations for the two workflows.
- The Open-Meteo supplement workflow SHOULD prioritize locations with no Open-Meteo supplement rows first and then locations whose Open-Meteo-backed forecast rows were updated longest ago.

## Impact

- Affected modules: `openspec/specs/forecast-cache-refresh/spec.md`, `openspec/specs/worker-schedule-orchestration/spec.md`, `src/WindLordApi.Worker`, `src/WindLordApi.Data`, `src/WindLordApi.Tests`
- Operational impact: forecast refresh becomes two scheduled jobs with different cadences instead of one combined five-minute workflow.
- Persistence impact: the forecast-cache upsert contract must explicitly preserve Yr precedence for overlapping timestamps and allow later Yr writes to supersede overlapping Open-Meteo rows.
- Query impact: the current shared location-selection strategy must diverge so the Open-Meteo supplement cadence optimizes future-horizon coverage through Open-Meteo-specific freshness signals rather than only generic forecast `updated_at` freshness.
- Configuration impact: no new provider credentials are expected, but schedule ownership and service naming will change in worker orchestration.
- Out of scope: changing provider mappings unrelated to forecast refresh, adding Open-Meteo landing forecasts, changing the existing startup health-check posture, or altering non-forecast worker schedules outside the MetYr/Open-Meteo split.
