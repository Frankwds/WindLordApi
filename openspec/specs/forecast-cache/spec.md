<!-- graphify-metadata
community: fallback (low-signal-ast-graph)
god_nodes: WindLordApi.Worker, ApplicationDbContext
bridge_nodes: ForecastUpdateService
related_modules: src/WindLordApi.Worker, src/WindLordApi.Data, src/WindLordApi.Integrations/MetYr
-->

# Forecast Cache

## Purpose
Refresh, prioritize, and retain forecast data for paragliding locations so downstream consumers have recent weather guidance without repeatedly calling the upstream forecast provider.

## Requirements

### Requirement: Expired forecasts MUST be removed before refresh work proceeds
The system MUST remove forecast records older than the configured retention window before persisting newly fetched forecast data.

#### Scenario: Starting a forecast refresh cycle with stale cache entries present
- Given the forecast cache contains entries older than the retention window
- When the forecast refresh workflow begins
- Then the stale entries are deleted before new forecast upserts are performed

### Requirement: Locations without forecasts SHALL be prioritized before stale locations
The system SHALL refresh locations with no existing forecast data before locations whose forecasts are merely older than others.

#### Scenario: Choosing the next batch of forecast refresh work
- Given some active locations have no forecast records and others have stale forecast records
- When the worker selects locations for the next refresh batch
- Then locations without forecasts are selected first

### Requirement: Forecast refresh SHALL process locations in bounded batches
The system SHALL refresh forecast data in bounded batches rather than attempting every location in a single cycle.

#### Scenario: Refreshing forecasts for many active locations
- Given more active locations need refresh than fit in a single cycle
- When the forecast update job selects work
- Then it processes only the configured batch size for that cycle

### Requirement: Persisted forecasts SHALL include both general and landing-oriented wind details
The system SHALL store forecast attributes needed for launch and landing interpretation, including wind direction, wind speed, gusts, and landing-related wind fields when provided.

#### Scenario: Mapping a provider forecast response into cache records
- Given a forecast response contains hourly weather details for a paragliding location
- When the response is mapped into cache records
- Then the stored forecast includes the wind and landing-specific fields required by the local forecast model