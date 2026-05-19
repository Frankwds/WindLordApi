<!-- graphify-metadata
community: fallback (low-signal-ast-graph)
god_nodes: ApplicationDbContext, WindLordApi.Worker
bridge_nodes: CountryLocatorService
related_modules: src/WindLordApi.Data, src/WindLordApi.Worker, src/WindLordApi.Integrations/GoogleGeocoding
-->

# Location Management

## Purpose
Maintain the metadata that describes paragliding locations and enrich weather-station records with geographic country information when that metadata is missing.

## Requirements

### Requirement: Paragliding locations SHALL preserve flight-direction suitability metadata
The system SHALL store directional suitability flags and core location metadata for paragliding locations so forecast and station consumers can interpret the location correctly.

#### Scenario: Persisting a paragliding location
- Given a paragliding location includes coordinates and supported wind directions
- When the location is stored or updated
- Then the persisted record retains its directional flags and core geographic metadata

### Requirement: Location records SHALL distinguish active and primary sites
The system SHALL retain flags that distinguish whether a location is active and whether it is considered a primary site.

#### Scenario: Marking a location inactive
- Given a location should remain in the system but no longer participate in active workflows
- When its activity metadata is updated
- Then the record remains stored with an inactive state rather than being implied as active

### Requirement: Weather stations missing country metadata SHALL be enriched from geographic coordinates
The system SHALL periodically look up country information for weather stations that do not already have country metadata.

#### Scenario: Running country enrichment for stations without country data
- Given persisted weather stations exist without a country value but with coordinates
- When the country enrichment workflow executes
- Then the system attempts to resolve and store country information for those stations

### Requirement: Country enrichment SHALL be schedulable as an operational workflow
The system SHALL support running the country enrichment workflow as part of startup and/or scheduled background processing.

#### Scenario: Service startup with unresolved station geography
- Given the service starts while some stations still lack country metadata
- When startup jobs execute
- Then the country enrichment workflow can be invoked without requiring a separate manual process