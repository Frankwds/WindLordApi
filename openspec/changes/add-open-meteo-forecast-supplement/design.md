## Context

WindLordApi already has a clear runtime split between Integrations, Worker, and Data. Forecast refresh orchestration belongs in Worker, provider-specific HTTP and mapping belong in Integrations, and forecast-cache uniqueness and upsert semantics belong in Data. The current checked-in forecast refresh flow is MetYr-only, with optional Yr landing enrichment before a per-location upsert.

The Open-Meteo addition should extend that existing flow rather than introduce a second forecast-writing pipeline. Yr remains authoritative for overlapping timestamps, landing forecast data remains Yr-only, and the existing `(LocationId, Time)` forecast-cache upsert remains the single persistence seam. The new requirement is to fetch one batched Open-Meteo takeoff supplement for the selected location set, then append only later timestamps to the per-location Yr results.

## Goals / Non-Goals

**Goals:**
- Keep `ForecastUpdateService` as the orchestration seam for location selection, provider coordination, merge rules, and persistence.
- Fetch Open-Meteo for all selected takeoff coordinates in one request up front while keeping MetYr requests sequential per location.
- Preserve Yr as the preferred provider by treating Open-Meteo as a supplement only for timestamps strictly later than the latest Yr timestamp returned for each location in the current run.
- Keep landing forecast fields sourced only from Yr.
- Normalize Open-Meteo WMO weather codes into the existing Yr-style weather-code vocabulary expected by WindLord.
- Add Open-Meteo startup validation and advisory health reporting consistent with the other integrations.

**Non-Goals:**
- Reintroduce the removed Open-Meteo design that blended atmospheric and pressure-level fields into the forecast cache.
- Add Open-Meteo as a separate scheduled workflow or persistence path outside `ForecastUpdateService`.
- Supplement landing coordinates from Open-Meteo.
- Introduce a schema change, migration, or new forecast table.
- Replace sequential MetYr fetches with a fully parallel multi-provider fan-out.

## Architecture

```text
                     forecast refresh run
                              |
                              v
                select candidate active main locations
                              |
                              v
         issue one batched Open-Meteo takeoff request up front
                              |
                              |
                              +-----------------------------+
                              |                             |
                              v                             |
                 sequential Yr per-location loop            |
                 - fetch Yr takeoff                         |
                 - map Yr rows                              |
                 - optionally fetch Yr landing              |
                 - merge landing wind fields                |
                 - record latest Yr timestamp               |
                 - if Yr fails, mark location skipped       |
                              |                             |
                              +-------------+---------------+
                                            |
                                            v
                              await Open-Meteo batch result
                                            |
                                            v
                          correlate response blocks to locations
                          - request order is primary
                          - returned coords are a sanity check
                                            |
                                            v
                             per-location merge and upsert
                             - keep all Yr rows
                             - append only later Open-Meteo rows
                             - set IsYrData per provider row
                             - skip locations whose Yr failed
```

## Orchestration Design

### Selection and fetch sequencing

The existing location-selection behavior remains unchanged:

1. delete expired forecast rows
2. select up to 50 candidate location ids, prioritizing missing coverage before stale coverage
3. materialize active main paragliding locations by id

Once the selected locations are available, `ForecastUpdateService` should start the Open-Meteo batch immediately for the full selected takeoff coordinate set. This satisfies the user decision that the batch include all selected locations up front.

MetYr should remain sequential per location. The service should continue iterating locations one at a time for:

- Yr takeoff fetch and mapping
- optional Yr landing fetch and merge
- local failure isolation and logging

This preserves the current operational profile while still overlapping the long-range supplement request with the MetYr loop.

### Merge control flow

The controlling rule is per-location Yr success.

- If Yr succeeds for a location, the workflow produces the Yr-derived forecast rows for that location.
- If Yr fails for a location, the workflow logs the failure and skips persistence for that location even if Open-Meteo succeeded.
- If the Open-Meteo batch fails, is partial, or is otherwise unusable, the workflow logs that batch failure once and still persists the Yr-derived rows for every location whose Yr processing succeeded.

This means the merge step is not a symmetric provider join. It is an authoritative primary result with an optional later-horizon append.

## Open-Meteo Integration Design

### Request shape

The Open-Meteo integration should call the generic forecast endpoint:

- base URL: `https://api.open-meteo.com/v1/forecast`
- `latitude`: comma-separated takeoff latitudes in request order, with each coordinate truncated to three decimals before serialization
- `longitude`: comma-separated takeoff longitudes in matching request order, with each coordinate truncated to three decimals before serialization
- `hourly`: `temperature_2m,wind_speed_10m,wind_direction_10m,wind_gusts_10m,precipitation,precipitation_probability,pressure_msl,weather_code,is_day`
- `start_hour`: rolling UTC start boundary representing exactly 48 hours ahead of the current run time
- `end_hour`: rolling UTC end boundary representing four days ahead of the current run time
- `wind_speed_unit=ms`
- `timezone=GMT`

The integration should return one mapped result per requested takeoff location.

Coordinate truncation is an explicit part of the request contract for this change. The worker should truncate, not round, takeoff coordinates to three decimals before building the request URL. This keeps request URLs smaller and improves the chance of Open-Meteo cache reuse without materially affecting forecast retrieval.

### Response correlation

For batched responses, correlation should follow this order:

1. request order is the primary contract
2. returned latitude and longitude are used as a sanity check against the corresponding truncated requested location

The workflow should not rely exclusively on any provider-supplied `location_id` field.

If the response count does not match the request count, if the batch is partial, or if returned coordinates materially disagree with the requested location order, the batch should fail rather than silently mis-assign forecast data. A failed or unusable batch should be dropped entirely and the workflow should continue with Yr-only persistence.

### Open-Meteo row mapping

Each hourly Open-Meteo row should map only the currently used takeoff forecast fields:

- `Time`
- `LocationId`
- `Temperature`
- `WindSpeed`
- `WindDirection`
- `WindGusts`
- `Precipitation`
- `PrecipitationProbability`
- `PressureMsl`
- `WeatherCode`
- `IsDay`
- `IsYrData = false`

Mapped numeric values should be rounded to the precision already enforced by the destination forecast-cache columns:

- temperature, wind speed, wind gusts, and pressure: one decimal place
- precipitation: two decimal places
- precipitation probability: keep the existing destination precision expectations used by the shared model

The following remain unset for Open-Meteo rows in this change:

- landing fields
- precipitation min and max
- atmospheric pressure-level fields
- cloud cover levels
- CAPE, convective inhibition, lifted index, boundary layer height, freezing level height, and geopotential heights

## Weather-Code Normalization

Open-Meteo returns WMO `weather_code` values plus `is_day`. WindLord expects the existing Yr-style symbol vocabulary, so Open-Meteo needs a dedicated mapping layer.

Locked mapping for this change:

- `0` -> `clearsky_day` or `clearsky_night`
- `1`, `2` -> `partlycloudy_day` or `partlycloudy_night`
- `3` -> `cloudy`
- `45`, `48` -> `fog`
- `51`, `53`, `55` -> `rain`
- `56`, `57` -> `sleet`
- `61`, `63`, `65` -> `rain`
- `66`, `67` -> `sleet`
- `71`, `73`, `75`, `77` -> `snow`
- `80`, `81`, `82` -> `rain`
- `85`, `86` -> `snow`
- `95`, `96`, `99` -> `rainandthunder`
- any other code -> `null`

`is_day` is only used to choose the day or night variant for WMO codes `0`, `1`, and `2`. All other mapped target codes are time-agnostic.

## Merge Rules

The per-location merge should use canonical UTC `DateTime` values, not string slices.

For each location with successful Yr processing:

1. determine the latest Yr timestamp returned for that location in the current run
2. take the mapped Open-Meteo rows for that same location
3. keep only rows whose `Time` is strictly later than the latest Yr timestamp
4. append those rows to the Yr-derived takeoff and optional landing-enriched rows
5. upsert the single merged batch for that location

This avoids conflicting writes for overlapping horizons and keeps the data-layer upsert contract unchanged.

## Startup Registration And Health

The Open-Meteo integration should follow the same registration pattern as the existing forecast integrations:

- configuration-bound options with startup validation
- typed `HttpClient`
- provider mapping registration
- dedicated startup health check

The startup health-check set should expand to include Open-Meteo, but the startup behavior remains advisory:

- the overall report still logs aggregate and per-check results
- an unhealthy Open-Meteo result does not block worker startup on its own

## Risks / Trade-offs

- Starting the Open-Meteo batch up front means the service may fetch supplement data for a location whose Yr fetch later fails. This is acceptable because the batched request is the desired shape and the merge step discards those results.
- Response correlation by order is simple and robust only if the batch client treats request ordering as part of its explicit contract and the implementation consistently compares returned coordinates against the truncated request coordinates. Tests need to lock that down.
- Sequential MetYr plus batched Open-Meteo is intentionally asymmetric. It keeps the existing control flow familiar, but it does not minimize total provider latency as aggressively as a fully parallel design would.
- Unknown WMO codes remain null by design. That is safer than inventing unsupported Yr-style symbols, but it means some future provider codes will surface as blank weather codes until the mapping table is expanded.

## Validation Strategy

Implementation should be considered complete only when it includes:

- unit coverage for Open-Meteo request construction and response correlation
- unit coverage for WMO-to-Yr weather-code mapping, including null behavior for unsupported codes
- unit or service coverage for per-location merge rules, `IsYrData` semantics, and the "Yr succeeds / Open-Meteo fails" plus "Yr fails / Open-Meteo succeeds" cases
- worker startup coverage for Open-Meteo option validation and advisory health-check registration
- executable validation with `openspec validate --specs`, `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`, and `dotnet build WindLordApi.sln` once implementation is finished