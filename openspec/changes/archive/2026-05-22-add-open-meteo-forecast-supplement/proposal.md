## Why

WindLordApi currently refreshes forecast cache rows exclusively from MetYr. That data is the preferred forecast source and should remain authoritative wherever it is available, but the current MetYr hourly horizon only reaches roughly 48 to 56 hours ahead. WindLord needs a longer hourly forecast range without sacrificing the higher-quality Yr coverage that already exists.

Open-Meteo fits this need because it can return forecast data for multiple coordinates in a single request and has broader geographic coverage for northern Norway. However, Open-Meteo still cannot be treated as an independent forecast-writing workflow, because forecast cache rows are authoritative by `(LocationId, Time)` and later writes replace the mutable payload for overlapping timestamps. This change keeps forecast-provider orchestration inside `ForecastUpdateService`, keeps Open-Meteo-specific HTTP and mapping behavior inside Integrations, and preserves the existing Data-layer persistence contract while extending the forecast horizon through day 4.

## What Changes

- Add Open-Meteo as a supported forecast integration with its own options, client, DTOs, mapping, and DI registration in `src/WindLordApi.Integrations`.
- Update `ForecastUpdateService` to fetch MetYr forecasts sequentially per selected paragliding location while also issuing one batched Open-Meteo takeoff forecast request up front for the same selected location set, then merge provider results into authoritative per-location forecast batches before upsert.
- Query the Open-Meteo `/v1/forecast` endpoint using comma-separated coordinate lists truncated to three decimals, a rolling UTC `start_hour` / `end_hour` window beginning 48 hours from the current run time, `timezone=GMT`, `wind_speed_unit=ms`, and the exact hourly variable set needed for the currently persisted Yr-equivalent takeoff fields: `temperature_2m`, `wind_speed_10m`, `wind_direction_10m`, `wind_gusts_10m`, `precipitation`, `precipitation_probability`, `pressure_msl`, `weather_code`, and `is_day`.
- Persist only Open-Meteo timestamps that are strictly later than the latest Yr timestamp returned for each location in the current refresh run.
- Preserve Yr as the preferred provider by writing Yr-only rows when the Open-Meteo batch fails, and by skipping persistence for a location when Yr fails even if Open-Meteo returned supplemental rows for it.
- Keep Open-Meteo supplementation limited to takeoff coordinates; landing forecasts continue to come only from Yr when landing coordinates exist.
- Set `IsYrData` accurately on merged rows, map Open-Meteo `weather_code` plus `is_day` values into the locked Yr-compatible weather-code vocabulary expected by WindLord, keep unknown or unsupported code combinations as null, and round mapped numeric values to match the forecast-cache column precision.
- Leave `precipitation_max` and `precipitation_min` unset for Open-Meteo rows because the Open-Meteo hourly forecast API does not expose hourly min/max precipitation fields analogous to the current Yr values.
- Add Open-Meteo to startup health checks with the same advisory startup behavior currently used for MetYr and the other registered integrations.
- Extend OpenSpec requirements and automated coverage for batched Open-Meteo fetches, response-to-location correlation, merged forecast refresh, provider precedence, WMO weather-code normalization, `IsYrData` semantics, and Open-Meteo startup health reporting.

## Capabilities

- Forecast refresh SHALL treat MetYr as the preferred forecast provider and SHALL supplement it with Open-Meteo only for timestamps strictly after the latest Yr timestamp returned for the same location in the current refresh run.
- Forecast refresh SHALL issue one up-front batched Open-Meteo request for all selected takeoff coordinates, truncated to three decimals, while still fetching MetYr per location and persisting one merged forecast batch per location through the existing forecast-cache upsert flow.
- When the batched Open-Meteo request fails, is partial, or is otherwise unusable but MetYr succeeds, the system SHALL still persist the Yr-derived forecast rows for the affected locations.
- When MetYr fails for a location, the system SHALL skip persistence for that location even if Open-Meteo returned supplemental rows for it.
- Open-Meteo-supplemented rows SHALL contain takeoff forecast data only, SHALL set `IsYrData = false`, and SHALL use the existing weather-code vocabulary consumed by WindLord.
- Open-Meteo weather normalization SHALL derive existing app weather codes from WMO `weather_code` values plus `is_day`, and SHOULD leave unknown or unsupported code combinations unset instead of coercing them.
- Open-Meteo field selection SHALL match the currently persisted Yr takeoff fields for temperature, surface wind, gusts, precipitation amount, precipitation probability, pressure, weather code, and day/night flag; fields not currently persisted from Yr SHOULD NOT be requested just for shape parity.
- Startup health checks SHALL include Open-Meteo alongside database, MetFrost, Holfuy, MetYr, and PortWind, and SHALL remain advisory rather than blocking worker execution on unhealthy results.

## Impact

- Affected modules: `openspec/specs/forecast-cache-refresh/spec.md`, `openspec/specs/runtime-bootstrap-and-health/spec.md`, `src/WindLordApi.Integrations`, `src/WindLordApi.Worker`, `src/WindLordApi.Tests`
- Operational impact: each forecast-refresh batch now performs dual-provider coordination, one batched Open-Meteo supplement request, and one additional provider health check during startup reporting.
- Data model impact: no schema or migration change is planned; the change reuses the existing `forecast_cache` model and the `IsYrData` column.
- Configuration impact: add Open-Meteo configuration for base URL and HTTP behavior. No provider credential is expected for the current non-commercial usage model.
- Out of scope: changing location-selection views, adding Open-Meteo landing forecasts, changing the existing forecast-cache cleanup retention rule, or reintroducing the earlier atmospheric-field enrichment design.