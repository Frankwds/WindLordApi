## Context

WindLordApi already separates provider responsibilities across Integrations, Worker, and Data. Provider-specific HTTP and parsing concerns belong in Integrations, orchestration and schedules belong in Worker, and persistence rules belong in Data. PortWind should follow that pattern, but it exposes a weakness in the current data-layer seam: weather station lifecycle operations are currently shaped around MET-specific methods even though the underlying persistence model is provider-agnostic.

PortWind has two distinct workflows. Its station catalog is published as a JavaScript assignment to `window.stations`, and its latest observation endpoint must be called one station at a time. The provider also publishes station readiness through `status` and `history`, which means PortWind active state should be treated as provider-authoritative rather than inferred from whether local station data exists.

## Goals / Non-Goals

**Goals:**
- Add PortWind as a supported provider while preserving the existing Worker -> Integrations -> Data boundaries.
- Generalize weather station lifecycle operations from MET-specific methods to provider-scoped APIs that future providers can also use.
- Parse PortWind station metadata safely from the JavaScript payload without executing remote script.
- Run PortWind station maintenance weekly and on startup, then run PortWind latest-data sync on startup and hourly using only active PortWind station ids.
- Keep the existing WeatherStation, StationData, and LatestStationData persistence model and upsert flow.
- Add focused automated coverage for parser robustness, provider-scoped active-state updates, startup order, and per-station observation failure tolerance.

**Non-Goals:**
- Introduce a generic provider plug-in framework across the entire application.
- Persist every PortWind-only field when the shared persistence model does not need it.
- Add an initial schema or migration change for PortWind-specific metadata.
- Use observation failures or empty latest-data responses as the source of truth for PortWind station activity.

## Architecture

```text
				WEEKLY / STARTUP
	https://portwind.no/js/stations.js
			  |
			  v
	    PortWind station client
    - fetch raw bytes
    - decode UTF-8 explicitly
    - extract window.stations object only
    - parse without executing script
			  |
			  v
	    PortWind station mapping
    - label cleanup from label only
    - Provider = PortWind
    - IsActive = status && history
    - default inactive when either missing
			  |
			  v
	 PortWind station refresh service
    - upsert current stations
    - activate/inactivate seen stations
    - deactivate missing PortWind stations

				 HOURLY / STARTUP
  https://portwind.no/api/v1/dbdata.php?stationid=...&dataset=latest
			  |
			  v
	   PortWind observation client
    - one request per active station id
    - continue on per-station failures
			  |
			  v
	  PortWind observation mapping
    - UpdatedAt = last_measurement
    - gust = wind_gust ?? wind_speed_max
    - shared StationData shape only
			  |
			  v
	StationDataService + LatestStationDataService
```

## Provider-Scoped Data Layer Changes

The main architectural change is to generalize weather station lifecycle methods so they are scoped by provider rather than by MET.

### Proposed service and repository seam

The data layer should expose provider-scoped operations along these lines:

- `GetActiveStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default)`
- `GetInactiveStationIdsByProviderAsync(string provider, CancellationToken cancellationToken = default)` if still needed by existing flows
- `SetStationsActiveByProviderAsync(string provider, IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken = default)`
- `SetStationsInactiveByProviderAsync(string provider, IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken = default)`
- `SetMissingStationsInactiveByProviderAsync(string provider, IReadOnlyCollection<string> seenStationIds, CancellationToken cancellationToken = default)`

This is the larger change slice the user selected. It removes the need to keep adding provider-specific repository methods and allows PortWind to fit the same persistence boundary as MET without baking PortWind logic into Data.

### MET migration within the same slice

Existing MET workflows should be updated to use the provider-scoped methods where they overlap with current behavior. That keeps the abstraction honest and avoids leaving both generalized and MET-specific station lifecycle methods side by side.

## PortWind Integration Design

## External API Contract Assumptions

The implementation depends on two upstream PortWind endpoints.

### Station catalog endpoint

- URL: `https://portwind.no/js/stations.js`
- Response type: JavaScript source, not JSON
- Contract assumption: the response contains a `window.stations = { ... }` assignment followed by additional JavaScript that is not part of the station catalog

The required downstream shape is the top-level station object keyed by station id, where only the following fields are required:

```javascript
window.stations = {
	"VS1285": {
		status: true,
		history: true,
		label: "Geiranger (N)",
		location: {
			lat: 62.102889,
			lng: 7.206366
		}
	}
};
```

Required fields from this payload:

- top-level station id key, for example `VS1285`
- `label`
- `status`
- `history`
- `location.lat`
- `location.lng`

All other fields, such as `maintenance`, `message`, `type`, `labeltooltip`, `camera`, `model`, `connection`, `sensors`, and any future additions, are optional and MUST be ignored unless a later requirement explicitly introduces downstream use for them.

### Latest observation endpoint

- URL pattern: `https://portwind.no/api/v1/dbdata.php?stationid=<station-id>&dataset=latest`
- Response type: JSON
- Request model: one station id per request

The required downstream shape is:

```json
{
	"server_time": 1779181147000,
	"last_measurement": 1779177600000,
	"data": [
		{
			"uts": 1779177600000,
			"temperature_min": 16,
			"temperature_avg": 16.6,
			"temperature_max": 16.8,
			"wind_direction_avg": 172,
			"wind_speed_avg": 3.1,
			"wind_speed_max": 9.2,
			"wind_gust": 8.2,
			"pressure_avg": 1014
		}
	]
}
```

Required fields from this payload:

- `last_measurement`
- `data[0].wind_speed_avg`
- `data[0].wind_direction_avg`
- `data[0].wind_gust` or `data[0].wind_speed_max`
- `data[0].temperature_avg`

Contract assumptions for this payload:

- `last_measurement` is the authoritative timestamp for persisted `UpdatedAt`
- timestamps are epoch milliseconds in UTC
- `data` may be empty, in which case the observation sync skips persistence for that station without changing station activity
- fields such as `server_time`, `uts`, `temperature_min`, `temperature_max`, and `pressure_avg` are optional for downstream behavior and are ignored unless a later requirement uses them
- `wind_gust` is preferred for gust mapping, with `wind_speed_max` as fallback
- fewer than 50 active stations are expected, so sequential requests without explicit rate limiting are operationally acceptable for the current design

### Station catalog parsing

The PortWind station catalog client should:

1. Fetch the response as raw bytes.
2. Decode using UTF-8 explicitly.
3. Locate the `window.stations =` assignment.
4. Extract the balanced object literal assigned to that symbol.
5. Parse the object literal as data without executing arbitrary JavaScript.
6. Ignore trailing script content after the object assignment.

If the object cannot be fully extracted or parsed, the station refresh must fail before any activity updates are applied. Partial station parses are not acceptable because the station refresh is authoritative for both activity state and missing-station deactivation.

### Station mapping rules

Only the fields needed downstream should be required:

- station id from the top-level object key
- `label`
- `status`
- `history`
- `location.lat`
- `location.lng`

Everything else should be treated as optional and ignored unless a later requirement needs it.

Station name should come from cleaned `label` only. The cleanup path should be conservative:

1. trim and normalize whitespace
2. preserve valid UTF-8 labels as-is
3. apply targeted mojibake repair only when the decoded label still contains known broken sequences and the repaired value is clearly better
4. fall back to station id only if the label is absent or unusable

### Active-state rules

For each station present in the latest PortWind catalog:

- `IsActive = true` only when `status == true` and `history == true`
- missing `status` means inactive
- missing `history` means inactive
- either field explicitly `false` means inactive

For PortWind stations already persisted but absent from the latest catalog:

- set `IsActive = false`

This keeps provider membership and provider readiness separate, while still using the station refresh as the source of truth for PortWind lifecycle state.

## PortWind Worker Design

### PortWind Station Refresh

The weekly and startup station refresh should:

1. Fetch and parse the full PortWind station catalog.
2. Map all valid stations to WeatherStation records with `Provider = "PortWind"`.
3. Upsert those WeatherStation records first.
4. Split seen station ids into active and inactive sets based on `status` and `history`.
5. Apply provider-scoped active and inactive updates for the seen station ids.
6. Mark missing persisted PortWind stations inactive.

This ordering preserves the repository invariant that metadata exists before dependent observation persistence and ensures reappearing stations can be reactivated by the same refresh.

### PortWind Latest-Data Sync

The startup and hourly latest-data sync should:

1. Query active PortWind station ids from the provider-scoped data-layer method.
2. Fetch `dataset=latest` one station at a time.
3. Continue after per-station failures.
4. Skip persistence for empty or malformed observation payloads without changing station activity.
5. Map the latest payload into shared StationData records.
6. Upsert StationData first, then derive and upsert LatestStationData.

### Observation mapping rules

The latest payload should map into the shared model as follows:

- `UpdatedAt = last_measurement`
- `WindSpeed = wind_speed_avg`
- `WindGust = wind_gust` with `wind_speed_max` as fallback
- `Direction = wind_direction_avg`
- `Temperature = temperature_avg`
- `WindMinSpeed = null`

Only the normalized current observation should be persisted. No extra PortWind-only helper fields should be stored in the shared tables.

## Scheduling And Startup

PortWind should be added to startup jobs in this order:

1. PortWind station refresh
2. PortWind latest-data sync

For recurring schedules, the proposed slots are:

- weekly station refresh: Sunday after the existing MET and country-location Sunday maintenance window, for example `0 0 6 * * SUN`
- hourly latest-data sync: near the top of the hour but offset from existing jobs, for example `0 3 * * * *`

The exact expressions can be tuned during implementation, but the design intent is to avoid simultaneous starts with the existing `:00`, `:01`, and `:02` jobs while still staying close to the provider's whole-hour refresh cadence.

## Risks / Trade-offs

- Generalizing station lifecycle APIs broadens the change beyond PortWind alone. This is intentional, but it increases the regression surface for MET workflows.
- The station catalog parser is sensitive to upstream format drift. Tests must cover trailing script, nested objects, missing optional fields, and malformed payload failure behavior.
- Conservative label repair may still miss some malformed names. That is preferable to aggressive rewriting that could corrupt valid labels.
- Sequential per-station latest-data polling is operationally acceptable at the current provider size, but the runtime should still be measured during implementation.

## Validation Strategy

Implementation should be considered complete only when it includes:

- unit coverage for station-catalog extraction and parsing
- unit coverage for label cleanup and active-state derivation
- unit coverage for latest-data mapping, including gust fallback and `last_measurement` timestamps
- repository or service tests for provider-scoped station lifecycle operations
- worker or service tests for startup order and per-station observation failure tolerance
- executable validation with `openspec validate --specs`, `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`, and `dotnet build WindLordApi.sln`