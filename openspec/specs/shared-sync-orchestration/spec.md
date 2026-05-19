<!-- graphify-metadata
community: 20 (service-layer) + 34 (sync-contracts) + 5 (repository-abstraction)
god_nodes: ForecastCacheService, StationDataService, WeatherStationService, IRepository
bridge_nodes: ILogger, IUnitOfWork
related_tables: forecast_cache, weather_stations, station_data, latest_station_data, all_paragliding_locations
-->

# Shared Sync Orchestration

## Purpose
Coordinate the worker's recurring sync responsibilities so each provider workflow remains isolated, validated, and consistent with the repository and service boundaries.

## Requirements

### Requirement: Keep Sync Responsibilities Separate
The worker SHALL schedule and execute forecast, provider sync, and location-enrichment workflows as separate responsibilities rather than collapsing them into a single all-purpose job.

#### Scenario: The worker starts its recurring jobs
Given the worker host has started and registered its scheduled services
When recurring work is scheduled
Then each sync responsibility SHALL run through its own workflow-specific service

### Requirement: Execute Syncs With Validated Provider Configuration
Each provider-backed sync MUST run through a registered client and validated options object before external calls are attempted.

#### Scenario: A sync service is resolved from dependency injection
Given a provider sync service is about to execute
When it uses its external client configuration
Then the workflow MUST rely on the configured options and registered client abstraction for that provider

### Requirement: Persist Writes Through Repository And Unit-Of-Work Boundaries
Sync workflows SHALL persist database changes through repository services and the unit-of-work boundary rather than direct orchestration-layer data access.

#### Scenario: A sync workflow needs to write normalized data
Given a workflow has mapped provider data into application models
When the workflow persists those changes
Then the writes SHOULD flow through repository and unit-of-work abstractions owned by the data layer