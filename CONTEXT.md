# WindLordApi

WindLordApi aggregates weather and location data for paragliding locations. This context file records the domain language used to describe provider data, station metadata, and observation workflows.

## Language

**Provider**:
An upstream external source of forecast, station, observation, or geocoding data.
_Avoid_: vendor, source system, feed

**WeatherStation**:
A provider-backed station metadata record used for observations.
_Avoid_: sensor row, source node, waypoint

**StationId**:
The globally unique identifier used to match a WeatherStation and its observation history inside WindLordApi.
_Avoid_: provider-scoped station key, local station id

**StationData**:
Historical normalized observation rows for a WeatherStation.
_Avoid_: live row, raw payload

**LatestStationData**:
The current snapshot derived from StationData history.
_Avoid_: canonical source table, live cache

**PortWind**:
A Provider that publishes weather-station metadata and weather-station observations.
_Avoid_: Port Wind, station feed

**PortWind-only Metadata**:
Provider fields from PortWind that are parsed from the source payload but are not part of WindLordApi's canonical WeatherStation or StationData model.
_Avoid_: shared station fields, persisted station contract

**PortWind Station Refresh**:
The workflow that synchronizes PortWind WeatherStation metadata and authoritative activity state into the canonical station registry.
_Avoid_: PortWind scrape, PortWind metadata job

**PortWind Observation Sync**:
The workflow that retrieves PortWind observations for WeatherStations already known to the system.
_Avoid_: PortWind polling job, PortWind data fetch

**PortWind Observation Failure**:
A failed PortWind request for one WeatherStation during observation polling.
_Avoid_: provider outage, station deactivation

**PortWind Station List Parse Failure**:
A failure to safely extract the PortWind station list into a complete set of provider station records.
_Avoid_: partial refresh, best-effort refresh

**PortWind Observation Timestamp**:
The measurement time taken from the PortWind observation row field `uts`.
_Avoid_: server time, payload time

**PortWind Temperature Reading**:
The normalized PortWind temperature value taken from `temperature_avg`.
_Avoid_: temperature minimum, temperature maximum

## Relationships

- A **Provider** can supply one or more **WeatherStation** records.
- A **WeatherStation** is matched in WindLordApi by a globally unique **StationId**.
- A **WeatherStation** can produce many **StationData** rows.
- **LatestStationData** is derived from **StationData**, not treated as an independent source.
- **PortWind Station Refresh** updates **WeatherStation** records for the **PortWind** provider and decides whether those stations remain active.
- **PortWind Station Refresh** can both deactivate missing PortWind stations and reactivate returning PortWind stations.
- For **PortWind**, activity is determined by presence in the latest station list, not by provider `status` or `maintenance` flags.
- **PortWind Observation Sync** reads **WeatherStation** records for the **PortWind** provider and writes **StationData**.
- A **PortWind Observation Failure** does not change **WeatherStation** activity state by itself.
- **PortWind-only Metadata** is ignored by WindLordApi behavior and persistence in the initial PortWind integration.
- A **PortWind Station List Parse Failure** invalidates the entire **PortWind Station Refresh**.
- A **PortWind Observation Timestamp** comes from the row-level PortWind `uts` field.
- A **PortWind Temperature Reading** comes from PortWind `temperature_avg`.

## Example dialogue

> **Dev:** "Should the **PortWind Observation Sync** create stations when it sees a new id?"
> **Domain expert:** "No. The **PortWind Station Refresh** owns **WeatherStation** creation, and the observation workflow only reads existing PortWind stations."

## Flagged ambiguities

- "PortWind sync" was being used to mean both station metadata refresh and observation ingestion. Resolved: use **PortWind Station Refresh** for metadata and **PortWind Observation Sync** for data ingestion.
- "active PortWind station" was ambiguous between last successful poll and current provider membership. Resolved: PortWind activity is defined by whether the station appears in the latest PortWind Station Refresh.
- "PortWind activity metadata" was ambiguous between list membership and provider `status`/`maintenance` flags. Resolved: only presence in the latest PortWind Station Refresh controls activity.
- "PortWind provider fields" was ambiguous between canonical model inputs and provider-only extras. Resolved: provider-only metadata such as `status`, `maintenance`, `message`, `camera`, `sensors`, and `history` may exist in the PortWind payload but are ignored by WindLordApi behavior and persistence.
- "PortWind parse failure" was ambiguous between skipping bad records and aborting the refresh. Resolved: if the station list cannot be safely parsed into a complete set of stations, the entire PortWind Station Refresh fails.
- "PortWind observation time" was ambiguous between `server_time`, `last_measurement`, and `data[].uts`. Resolved: WindLordApi uses `data[].uts` as the PortWind observation timestamp.
- "PortWind temperature" was ambiguous between `temperature_min`, `temperature_avg`, and `temperature_max`. Resolved: WindLordApi uses `temperature_avg` as the normalized PortWind temperature reading.
- "station identity" was ambiguous between `Provider + StationId` and a globally unique station key. Resolved: WindLordApi currently treats **StationId** as globally unique across providers.
- "PortWind reappearance" was ambiguous between creating a new station and reviving the existing one. Resolved: when a PortWind station reappears in a later refresh, the existing **WeatherStation** is reactivated.
- "PortWind observation result" was ambiguous between station availability and observation availability. Resolved: per-station observation failures are tolerated, and an empty observation payload does not make the station inactive.