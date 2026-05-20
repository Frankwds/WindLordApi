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
The system SHALL run high-frequency observation syncs separately from lower-frequency provider maintenance tasks such as station discovery and active-status refresh. When a provider depends on station maintenance to determine which stations are eligible for observation polling, the maintenance workflow SHALL be able to run before the frequent observation workflow during startup and on its own recurring schedule.

#### Scenario: Weekly provider maintenance run
- Given a provider supports periodic discovery or status-refresh workflows
- When the scheduled maintenance window executes
- Then the provider maintenance workflow runs without replacing the normal frequent observation sync cadence

#### Scenario: Startup maintenance precedes provider observation sync
- Given a provider requires persisted station maintenance results to decide which stations can be polled for observations
- When the worker runs startup jobs
- Then the provider maintenance workflow completes before the provider's observation sync begins

### Requirement: Provider station maintenance SHALL apply lifecycle changes within the owning provider scope
The system SHALL apply station maintenance changes only to weather stations owned by the provider whose maintenance workflow is executing. Active-state updates and missing-station deactivation for one provider MUST NOT alter weather stations belonging to another provider.

#### Scenario: Deactivating stations missing from a provider catalog
- Given persisted weather stations exist for multiple providers
- And one provider's latest maintenance payload no longer contains a previously persisted station from that same provider
- When the provider maintenance workflow applies the refreshed station set
- Then the missing station is marked inactive only for that provider
- And stations owned by other providers remain unchanged

### Requirement: Provider-maintained station readiness SHALL control whether observations are polled
When a provider publishes station readiness through maintenance metadata, the system SHALL use that provider-maintained readiness to decide which stations are eligible for observation polling. Stations that are missing from the current provider catalog or are marked unavailable by required provider readiness fields MUST remain persisted but inactive until a later maintenance run reactivates them.

#### Scenario: Required readiness metadata is missing or false
- Given a provider station remains present in the provider maintenance payload
- And one or more required readiness fields are missing or false
- When the maintenance workflow applies active-state updates
- Then the station remains persisted
- And the station is marked inactive
- And the frequent observation workflow does not poll that station

#### Scenario: A returning provider station is reactivated
- Given a provider station was previously persisted and inactive because it was missing or unavailable in an earlier maintenance run
- And the station appears again in a later maintenance payload with all required readiness conditions satisfied
- When the maintenance workflow applies the refreshed station state
- Then the station is marked active again

### Requirement: Non-JSON provider station catalogs SHALL be parsed as data without executing remote script
When a provider publishes station catalog data inside a non-JSON wrapper such as a JavaScript assignment, the system SHALL extract and parse only the data needed for downstream station maintenance. The maintenance workflow MUST ignore unrelated trailing script content and MUST fail without applying partial lifecycle updates if the required station catalog cannot be fully extracted or parsed.

#### Scenario: Parsing a JavaScript-wrapped station catalog
- Given a provider station catalog is published as a JavaScript assignment followed by additional script
- When the maintenance workflow extracts the station catalog for parsing
- Then only the assigned station data object is parsed for station maintenance
- And the trailing script content is ignored

#### Scenario: Rejecting a partially parsed station catalog
- Given a provider station catalog cannot be fully extracted or parsed into a complete station set
- When the maintenance workflow attempts to process that payload
- Then the workflow fails without applying partial active-state or missing-station updates