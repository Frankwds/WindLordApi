<!-- graphify-metadata
community: 52 (metfrost-sync) + 34 (sync-contracts)
god_nodes: IMetFrostSyncService, MetFrostSyncService, IWeatherStationService, WeatherStationService
bridge_nodes: IWeatherStationService (also in community 34)
related_tables: weather_stations
-->

# Weather Station Network

## Purpose
Maintain the canonical registry of weather stations used by the worker so provider metadata, active status, and station identity stay aligned with external sources.

## Requirements

### Requirement: Upsert Station Metadata Before Writing New Observations
Weather-station metadata MUST be written before new observation rows are stored for newly discovered provider stations.

#### Scenario: A provider returns a previously unknown station with observations
Given a provider payload containing a station that is not yet present in the database
When the sync workflow processes that payload
Then the weather-station record MUST be upserted before related observation rows are written

### Requirement: Preserve Provider Identity For Station Matching
Each weather station SHALL keep its provider identity and provider station identifier as the stable lookup key for repeated syncs.

#### Scenario: The same provider station is seen on a later sync
Given an existing weather-station record for a provider station identifier
When a later sync returns updated metadata for that same provider station
Then the existing weather-station record SHALL be updated instead of creating a duplicate station entry

### Requirement: Synchronize Station Activity Separately From Observation Ingestion
Station active-status maintenance SHALL remain a dedicated part of the station-network workflow instead of being implied only by observation ingestion.

#### Scenario: Station activity is refreshed independently
Given provider station metadata that reflects whether stations are active
When the station-network maintenance workflow runs
Then the worker SHOULD update station activity state even when no new observation data is being written