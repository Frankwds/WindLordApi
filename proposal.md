## Why

WindLordApi currently refreshes forecast cache rows exclusively from MetYr. That data is the preferred forecast source and should remain authoritative wherever it is available, but the current MetYr hourly horizon only reaches roughly 48 to 56 hours ahead. WindLord needs a longer hourly forecast range without sacrificing the higher-quality Yr coverage that already exists.

Open-Meteo fits this need because it can return forecast data for multiple coordinates in a single request and has broader geographic coverage for northern Norway. However, Open-Meteo still cannot be treated as a separate forecast-writing workflow, because `forecast_cache` rows are authoritative by `(LocationId, Time)` and later writes replace the mutable payload for overlapping timestamps. This change keeps forecast-provider orchestration inside `ForecastUpdateService`, keeps Open-Meteo-specific HTTP and mapping behavior inside Integrations, and preserves the existing Data-layer persistence contract while extending the forecast horizon through day 4.

## What Changes

- Add Open-Meteo as a supported forecast integration with its own options, client, DTOs, mapping, and DI registration in `src/WindLordApi.Integrations`.
- Update `ForecastUpdateService` to fetch MetYr forecasts per selected paragliding location while fetching one batched Open-Meteo takeoff forecast request for the same selected location set, then merge provider results into authoritative per-location forecast batches, merge Yr landing data only when landing coordinates exist, and upsert the merged rows through the existing forecast-cache service.
- Query the generic Open-Meteo `/v1/forecast` endpoint using comma-separated coordinate lists, a rolling UTC `start_hour` / `end_hour` window for the supplemental day-2-through-day-4 range, `timezone=GMT`, `wind_speed_unit=ms`, and the exact hourly variable set needed for the currently persisted Yr-equivalent takeoff fields: `temperature_2m`, `wind_speed_10m`, `wind_direction_10m`, `wind_gusts_10m`, `precipitation`, `precipitation_probability`, `pressure_msl`, `weather_code`, and `is_day`.
- Persist only Open-Meteo timestamps that are strictly later than the latest Yr timestamp returned for each location in the current run.
- Preserve Yr as the preferred provider by writing Yr-only rows when the Open-Meteo supplement fails, and by skipping the location entirely when Yr fails even if Open-Meteo succeeded.
- Keep Open-Meteo supplementation limited to takeoff coordinates; landing forecasts continue to come only from Yr when landing coordinates exist.
- Set `IsYrData` accurately on merged rows, map Open-Meteo `weather_code` plus `is_day` values into the locked Yr-compatible weather-code vocabulary expected by WindLord, and keep unknown or unsupported code combinations as null.
- Leave `precipitation_max` and `precipitation_min` unset for Open-Meteo rows unless the design is expanded, because the Open-Meteo hourly forecast API does not expose hourly min/max precipitation fields analogous to the current Yr values.
- Add Open-Meteo to startup health checks with the same advisory startup behavior currently used for MetYr and the other registered integrations.
- Extend OpenSpec requirements and automated coverage for batched Open-Meteo fetches, response-to-location correlation, merged forecast refresh, provider precedence, WMO weather-code normalization, `IsYrData` semantics, and Open-Meteo startup health reporting.

## Capabilities

- Forecast refresh SHALL treat MetYr as the preferred forecast provider and SHALL supplement it with Open-Meteo only for timestamps strictly after the latest Yr timestamp returned for the same location in the current refresh run.
- Forecast refresh SHALL fetch Open-Meteo for all selected takeoff coordinates in batched requests, but SHALL still persist a single merged forecast batch per location through the existing `ForecastCacheService` upsert flow.
- When a batched Open-Meteo request fails but MetYr succeeds, the system SHALL still persist the Yr-derived forecast rows for the affected locations.
- When MetYr fails for a location, the system SHALL skip persistence for that location even if Open-Meteo returned supplemental rows for it.
- Open-Meteo-supplemented rows SHALL contain takeoff forecast data only, SHALL set `IsYrData = false`, and SHALL use the existing weather-code vocabulary consumed by WindLord.
- Open-Meteo weather normalization SHALL derive existing app weather codes from WMO `weather_code` values plus `is_day`, and SHOULD leave unknown or unsupported code combinations unset instead of coercing them.
- Open-Meteo weather normalization SHALL use this locked mapping: `0 -> clearsky_day/night`, `1/2 -> partlycloudy_day/night`, `3 -> cloudy`, `45/48 -> fog`, `51/53/55 -> rain`, `56/57 -> sleet`, `61/63/65 -> rain`, `66/67 -> sleet`, `71/73/75/77 -> snow`, `80/81/82 -> rain`, `85/86 -> snow`, `95/96/99 -> rainandthunder`, and any other code -> `null`.
- Open-Meteo weather normalization SHALL use `is_day` only for codes `0`, `1`, and `2`; all other mapped target codes are time-agnostic.
- Open-Meteo field selection SHALL match the currently persisted Yr takeoff fields for temperature, surface wind, gusts, precipitation amount, precipitation probability, pressure, weather code, and day/night flag; fields not currently persisted from Yr SHOULD NOT be requested just for shape parity.
- Open-Meteo rows SHALL leave `precipitation_max` and `precipitation_min` null unless a separate enrichment rule is designed, because the hourly API does not provide those values.
- Open-Meteo response correlation SHALL NOT rely exclusively on a provider-supplied `location_id`; request order and returned coordinates SHALL be sufficient to map each response block back to the selected paragliding location.
- Startup health checks SHALL include Open-Meteo alongside database, MetFrost, Holfuy, MetYr, and PortWind, and SHALL remain advisory rather than blocking worker execution on unhealthy results.

## Impact

- Affected modules: `openspec/specs/forecast-cache-refresh/spec.md`, `openspec/specs/runtime-bootstrap-and-health/spec.md`, `src/WindLordApi.Integrations`, `src/WindLordApi.Worker`, `src/WindLordApi.Tests`
- Operational impact: no new cron schedule or startup job is introduced, but each forecast-refresh batch now performs dual-provider coordination, one or more batched Open-Meteo supplement requests, and an additional provider health check participates in startup reporting.
- Data model impact: no schema or migration change is planned; the change reuses the existing `forecast_cache` model and the `IsYrData` column.
- Configuration impact: add Open-Meteo configuration for base URL and HTTP behavior. No provider credential is expected for the current non-commercial usage model.
- Out of scope: changing location-selection views, adding Open-Meteo landing forecasts, or changing the existing forecast-cache cleanup retention rule.