## Context

WindLordApi already has a stable provider pattern across WindLordApi.Integrations and WindLordApi.Worker: provider-specific clients and mappings live in the integrations layer, provider sync services orchestrate work in the worker layer, and normalized persistence flows through WeatherStationService, StationDataService, and LatestStationDataService. PortWind fits the same domain but differs materially from the current Providers in two places: its station list is published as JavaScript that assigns an object to `window.stations` and continues with more script, and its observation endpoint must be called once per station id using the `latestandprevious` dataset.

The PortWind station payload also uses JavaScript object literal syntax rather than strict JSON, with unquoted property names and examples of mojibake in station labels. The implementation therefore needs a safe parsing path that does not execute remote JavaScript, plus a normalization step for station names before WeatherStation upserts. On the observation side, PortWind returns epoch-millisecond timestamps and helper fields such as `temperature_avg_previous` that are useful for comparison but are not independent StationData rows.

## Goals / Non-Goals

**Goals:**
- Add PortWind as a supported Provider without changing the existing worker, integrations, and data-layer boundaries.
- Parse PortWind station metadata safely from the JavaScript payload and normalize it into WeatherStation upserts keyed by the repository's existing globally unique station id model.
- Fetch PortWind observation data for active PortWind stations already in the database, normalize supported measurements and epoch-millisecond timestamps, and persist them through StationDataService and LatestStationDataService.
- Keep PortWind configuration validated at startup and register PortWind through explicit client, mapping, and workflow services.
- Add focused automated coverage for station-list parsing, label normalization, observation mapping, batching, reactivation and deactivation behavior, and metadata-before-observation persistence order.

**Non-Goals:**
- Introduce a generic plug-in framework for every Provider.
- Execute arbitrary JavaScript from the PortWind station-list source.
- Persist every PortWind-specific field when the current shared WeatherStation and StationData models do not need it.
- Add an initial database schema change for camera, sensors, history, or other PortWind-only metadata.

## Decisions

### Keep PortWind inside the existing provider integration pattern
PortWind will be introduced as its own integrations folder with a client abstraction, options type, DTOs, and mapping service. The worker will register PortWind options validation, the PortWind HTTP client, and two worker services: one for PortWind Station Refresh and one for PortWind Observation Sync.

This keeps provider-specific behavior isolated in WindLordApi.Integrations and preserves the current worker orchestration model instead of adding a new abstraction layer before the repository needs one.

Alternative considered: add a generic provider plug-in system first. Rejected because the current codebase already has a workable provider registration pattern, and PortWind is a concrete integration task rather than an architectural rework.

### Parse only the `window.stations` assignment and never execute the remote script
The PortWind client should download the raw station-list text, locate the `window.stations =` assignment, extract the balanced object literal assigned to it, and deserialize only that object. Trailing JavaScript after the object must be ignored, and the parser must tolerate unquoted property names without evaluating the script.

This approach keeps the integration safe and deterministic. It treats the payload as data to extract, not code to run.

Alternative considered: use a JavaScript engine or browser runtime to evaluate the remote file. Rejected because it adds unnecessary dependency and security risk for a payload whose useful content is limited to a single object assignment.

### Fail the entire PortWind Station Refresh when the station list cannot be parsed completely
PortWind activity decisions depend on comparing the full current provider list against the PortWind WeatherStations already in the database. If the station-list payload cannot be extracted or parsed into a complete set of stations, the refresh must stop without applying partial activity updates.

This keeps deactivation and reactivation semantics trustworthy. A partial parse would make missing stations indistinguishable from parser loss.

Alternative considered: keep parseable stations and skip the malformed remainder. Rejected because the PortWind Station Refresh is authoritative for activity state, so partial input would create false deactivations.

### Normalize PortWind labels before WeatherStation persistence
The PortWind mapping path should apply a targeted encoding-repair step to station labels before writing WeatherStation metadata. If the text contains known mojibake patterns such as `TromsÃ¸` or `BodÃ¸`, the mapper should repair the text to the intended UTF-8 representation before persistence. If a repair attempt does not improve the text, the original label should be preserved.

This keeps user-facing station names readable without widening the shared data model.

Alternative considered: persist raw PortWind labels exactly as received. Rejected because the observed payload already contains broken display text and would degrade station metadata quality.

### Derive PortWind activity from station-list membership plus `status` and `history`
The PortWind station-list payload should remain the source of truth for which PortWind stations currently exist, but WeatherStation activity should be determined by both station-list membership and the provider's `status` and `history` booleans. Existing PortWind stations missing from the current list should be marked inactive, and stations that remain in the list should only be marked active when both `status` and `history` are `true`. If either boolean is `false`, the station should remain in the database but be persisted as inactive.

This keeps recurring observation sync decisions tied to provider-declared station readiness rather than to list membership alone while still using a full station refresh as the authoritative source of station existence.

Alternative considered: continue treating list membership alone as active status. Rejected because PortWind already exposes provider booleans that distinguish stations which exist in the list from stations whose data should not currently drive observation ingestion.

### Split PortWind into a weekly station refresh and a separate active-station observation sync
PortWind should not follow the WindsMobi single-pass model. The worker should run a lower-frequency PortWind Station Refresh that updates metadata, deactivates missing stations, and reactivates returning stations, and a separate PortWind Observation Sync that reads active PortWind station ids from the database and polls only those stations.

This fits the user-selected operating model and keeps the observation workflow dependent on persisted active state rather than on a fresh provider station-list call every time.

Alternative considered: combine refresh and observations into one PortWind sync service. Rejected because the desired behavior explicitly uses two cadences and makes the refresh authoritative for activity.

### Fetch observations per active station id and persist only normalized current measurements
The PortWind observation path should read active PortWind station ids from the database, build one observation request per station id against `dataset=latestandprevious`, and segment those calls into bounded batches. The mapper should use `data[].uts` as the normalized observation timestamp, map `temperature_avg` into the shared temperature field, and ignore comparative `*_previous` fields as provider helpers rather than separate observations.

This preserves the existing observation-ingestion contract while accommodating the provider's per-station pull model.

Alternative considered: treat the `*_previous` values as additional StationData rows. Rejected because the endpoint shape looks like comparison metadata attached to the latest row, not an independent normalized observation history feed.

### Continue PortWind observation work on per-station failures
PortWind observation requests should be isolated enough that one failing station does not abort the rest of the PortWind Observation Sync. The workflow should continue with the remaining station ids, log the failed station requests, and leave station activity unchanged when a request fails or returns no rows.

This preserves useful ingestion progress even when some provider calls fail and keeps activity decisions inside the PortWind Station Refresh.

Alternative considered: fail the entire observation run on the first error, or mark stations inactive on empty responses. Rejected because PortWind observations are polled one station at a time and empty or failed observation responses are not authoritative for station activity.

## Risks / Trade-offs

- PortWind may change the shape of the JavaScript payload or the `window.stations` assignment format -> Mitigate with extractor tests that cover trailing JavaScript, unquoted keys, nested objects, and malformed payload failures.
- Mojibake repair can over-correct already valid labels -> Apply repair only when a round-trip improves the text and add mapping tests for both repaired and untouched labels.
- Per-station observation polling can increase runtime or stress the provider -> Keep request batches bounded, prefer provider-sized segmentation in the sync service, and make any concurrency limit explicit in PortWind options or constants.
- The current repository only exposes MET-specific active-station lookup methods -> Add provider-aware PortWind station-id queries without changing the globally unique station_id model or the existing schema.
- PortWind exposes station metadata that does not fit the current shared schema -> Keep unsupported fields in provider DTOs for now and defer schema changes until a separate requirement exists.

## Migration Plan

Add PortWind configuration binding, client registration, mapping, and split sync orchestration behind the normal worker startup path. Deploy the worker with the PortWind station-list URL, observation base URL, and schedule settings, then let the weekly PortWind Station Refresh establish active WeatherStations before the recurring PortWind Observation Sync depends on them.

Rollback remains a code-and-configuration rollback: disable or remove PortWind registration and configuration, then redeploy the previous worker version. No initial database migration is planned.

## Open Questions

None at this stage.