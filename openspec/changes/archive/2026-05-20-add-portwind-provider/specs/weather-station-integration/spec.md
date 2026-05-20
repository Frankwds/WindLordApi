## MODIFIED Requirements

### Requirement: Provider-specific sync schedules SHALL separate frequent data refresh from less frequent station maintenance
The system SHALL run high-frequency observation syncs separately from lower-frequency provider maintenance tasks such as station discovery and active-status refresh. When a provider depends on station maintenance to determine which stations are eligible for observation polling, the maintenance workflow SHALL be able to run before the frequent observation workflow during startup and on its own recurring schedule.

#### Scenario: Weekly provider maintenance run
- **GIVEN** a provider supports periodic discovery or status-refresh workflows
- **WHEN** the scheduled maintenance window executes
- **THEN** the provider maintenance workflow runs without replacing the normal frequent observation sync cadence

#### Scenario: Startup maintenance precedes provider observation sync
- **GIVEN** a provider requires persisted station maintenance results to decide which stations can be polled for observations
- **WHEN** the worker runs startup jobs
- **THEN** the provider maintenance workflow completes before the provider's observation sync begins

## ADDED Requirements

### Requirement: Provider station maintenance SHALL apply lifecycle changes within the owning provider scope
The system SHALL apply station maintenance changes only to weather stations owned by the provider whose maintenance workflow is executing. Active-state updates and missing-station deactivation for one provider MUST NOT alter weather stations belonging to another provider.

#### Scenario: Deactivating stations missing from a provider catalog
- **GIVEN** persisted weather stations exist for multiple providers
- **AND** one provider's latest maintenance payload no longer contains a previously persisted station from that same provider
- **WHEN** the provider maintenance workflow applies the refreshed station set
- **THEN** the missing station is marked inactive only for that provider
- **AND** stations owned by other providers remain unchanged

### Requirement: Provider-maintained station readiness SHALL control whether observations are polled
When a provider publishes station readiness through maintenance metadata, the system SHALL use that provider-maintained readiness to decide which stations are eligible for observation polling. Stations that are missing from the current provider catalog or are marked unavailable by required provider readiness fields MUST remain persisted but inactive until a later maintenance run reactivates them.

#### Scenario: Required readiness metadata is missing or false
- **GIVEN** a provider station remains present in the provider maintenance payload
- **AND** one or more required readiness fields are missing or false
- **WHEN** the maintenance workflow applies active-state updates
- **THEN** the station remains persisted
- **AND** the station is marked inactive
- **AND** the frequent observation workflow does not poll that station

#### Scenario: A returning provider station is reactivated
- **GIVEN** a provider station was previously persisted and inactive because it was missing or unavailable in an earlier maintenance run
- **AND** the station appears again in a later maintenance payload with all required readiness conditions satisfied
- **WHEN** the maintenance workflow applies the refreshed station state
- **THEN** the station is marked active again

### Requirement: Non-JSON provider station catalogs SHALL be parsed as data without executing remote script
When a provider publishes station catalog data inside a non-JSON wrapper such as a JavaScript assignment, the system SHALL extract and parse only the data needed for downstream station maintenance. The maintenance workflow MUST ignore unrelated trailing script content and MUST fail without applying partial lifecycle updates if the required station catalog cannot be fully extracted or parsed.

#### Scenario: Parsing a JavaScript-wrapped station catalog
- **GIVEN** a provider station catalog is published as a JavaScript assignment followed by additional script
- **WHEN** the maintenance workflow extracts the station catalog for parsing
- **THEN** only the assigned station data object is parsed for station maintenance
- **AND** the trailing script content is ignored

#### Scenario: Rejecting a partially parsed station catalog
- **GIVEN** a provider station catalog cannot be fully extracted or parsed into a complete station set
- **WHEN** the maintenance workflow attempts to process that payload
- **THEN** the workflow fails without applying partial active-state or missing-station updates