# Weather Station Lifecycle

## Purpose
This capability defines how weather stations are identified, refreshed, activated, deactivated, and enriched after provider mappings hand station records to the shared data layer. It is owned primarily by `ApplicationDbContext`, `WeatherStationRepository`, and `WeatherStationService`, and it is consumed by provider workflows such as MetFrost maintenance, PortWind station refresh, Holfuy sync, and country enrichment.

## Requirements

### Requirement: Weather stations SHALL use StationId as the durable identity
The system SHALL treat `StationId` as the durable identity for a weather station across provider sync and station-data relationships. Persisted weather stations SHALL remain unique by `StationId`, while the database primary key remains an internal identifier.

#### Scenario: Upserting an existing provider station
- **GIVEN** a weather station already exists with a persisted `StationId`
- **WHEN** a later provider sync upserts a station record with the same `StationId`
- **THEN** the existing weather station row is updated instead of creating a duplicate row
- **AND** dependent station data continues to relate to that station through `StationId`

### Requirement: Shared upsert SHALL refresh mutable metadata and preserve reserved fields on conflicts
The shared weather-station upsert path SHALL refresh mutable metadata such as name, coordinates, altitude, provider, and `UpdatedAt`. For matched rows, it SHALL preserve existing `Country` and `IsMain` values, and it SHALL preserve existing `IsActive` unless the incoming provider is Holfuy. Provider mappings MAY seed those fields when inserting a station for the first time.

#### Scenario: Updating an existing MET station
- **GIVEN** an existing MET weather station already has persisted `Country`, `IsMain`, and `IsActive` values
- **WHEN** normal metadata upsert runs for the same `StationId`
- **THEN** mutable metadata such as name and coordinates is refreshed
- **AND** the existing `Country`, `IsMain`, and `IsActive` values remain unchanged

#### Scenario: Re-seeing an existing Holfuy station
- **GIVEN** a persisted Holfuy weather station is inactive
- **WHEN** Holfuy metadata is upserted again for the same `StationId`
- **THEN** the matched weather station is marked active
- **AND** the existing `Country` and `IsMain` values are still preserved

### Requirement: Shared weather-station writes SHALL validate input and process bulk requests in batches
The shared weather-station service SHALL reject null or empty weather-station arrays, reject arrays containing only null elements, and require a non-empty provider name for provider-scoped lifecycle operations. For bulk weather-station writes, the service SHALL process the input in batches.

#### Scenario: Rejecting an invalid bulk write request
- **GIVEN** a caller passes a null, empty, or all-null weather-station array
- **WHEN** the shared weather-station service validates the request
- **THEN** the service rejects the request with an argument error before persistence starts

#### Scenario: Processing a large station refresh
- **GIVEN** a provider refresh produces more than 1000 weather stations to write
- **WHEN** the shared weather-station service persists the refresh
- **THEN** the service writes the refresh in multiple batches
- **AND** all valid stations still flow through the same shared lifecycle path

### Requirement: Provider-scoped active-state maintenance SHALL only mutate stations owned by that provider
The system SHALL expose provider-scoped active-state operations that only update weather stations whose `Provider` matches the requested provider. These operations SHALL support explicit activation and deactivation, deactivation of provider stations missing from a maintenance payload, and data-driven activation or deactivation for MET maintenance.

#### Scenario: Deactivating stations for one provider
- **GIVEN** persisted weather stations exist for multiple providers
- **AND** a provider maintenance workflow requests deactivation for a set of station ids
- **WHEN** the shared lifecycle applies that deactivation for one provider
- **THEN** only stations owned by that provider are marked inactive
- **AND** stations owned by other providers remain unchanged

#### Scenario: Deactivating provider stations missing from maintenance input
- **GIVEN** a provider maintenance workflow supplies the station ids seen in its latest catalog
- **WHEN** the shared lifecycle applies the missing-station operation for that provider
- **THEN** active stations owned by that provider and absent from the seen-station list are marked inactive

#### Scenario: MET active-status maintenance uses persisted station data
- **GIVEN** inactive `MET` weather stations exist and some of them already have persisted station data
- **WHEN** the MET active-status maintenance workflow runs
- **THEN** `MET` stations with persisted station data are marked active
- **AND** active `MET` stations without persisted station data are marked inactive

### Requirement: Missing-country enrichment SHALL use a dedicated update path
The system SHALL treat weather stations with `Country = null` or `Country = "UKJENT"` as candidates for country enrichment. The country-enrichment update path SHALL change only `Country` and `IsMain`, leaving other persisted weather-station fields unchanged.

#### Scenario: Selecting stations that need country enrichment
- **GIVEN** persisted weather stations include rows with `Country = null`, `Country = "UKJENT"`, and resolved country values
- **WHEN** the missing-country query runs
- **THEN** only the `null` and `"UKJENT"` stations are returned for enrichment

#### Scenario: Applying geocoded country results
- **GIVEN** a weather station already exists with provider metadata and active-state information
- **WHEN** the country-enrichment workflow persists a resolved country for that station
- **THEN** only `Country` and `IsMain` are updated
- **AND** name, coordinates, provider, and active-state remain unchanged