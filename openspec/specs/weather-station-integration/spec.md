<!-- graphify-metadata
community: fallback (low-signal-ast-graph)
god_nodes: WindLordApi.Worker, WindLordApi.Integrations, ApplicationDbContext
bridge_nodes: WindLordApi.Worker
related_modules: src/WindLordApi.Worker, src/WindLordApi.Integrations, src/WindLordApi.Data
-->

# Weather Station Integration

## Purpose
Synchronize weather-station metadata and observational data from external providers into the local persistence model used by the worker service.

## Requirements

### Requirement: Provider station metadata SHALL be available before observational data is persisted
The system SHALL ensure a provider station record exists before storing time-series observations for that station.

#### Scenario: Syncing provider data for a station that is not yet registered
- Given a provider response includes a station that is not yet present in persisted weather stations
- When a sync job processes that provider payload
- Then the weather station metadata is stored before dependent observation records are written

### Requirement: Station observations MUST be idempotent by station and timestamp
The system MUST treat a station observation as uniquely identified by station and observation timestamp so repeated sync runs do not create duplicates for the same sample.

#### Scenario: Reprocessing an already-seen observation
- Given an observation for the same station and timestamp already exists
- When the same observation is received in a later sync run
- Then the persisted observation set remains unique for that station and timestamp

### Requirement: Latest station data SHALL be maintained as a read-optimized projection
The system SHALL maintain a latest-station-data view or table that reflects the most recent observation for each station.

#### Scenario: Receiving a newer observation for an existing station
- Given a station already has a latest observation stored
- When a newer observation is ingested for that station
- Then the latest-station-data projection reflects the newer observation

### Requirement: Provider-specific sync schedules SHALL separate frequent data refresh from less frequent station maintenance
The system SHALL run high-frequency observation syncs separately from lower-frequency provider maintenance tasks such as station discovery and active-status refresh.

#### Scenario: Weekly provider maintenance run
- Given a provider supports periodic discovery or status-refresh workflows
- When the scheduled maintenance window executes
- Then the provider maintenance workflow runs without replacing the normal frequent observation sync cadence