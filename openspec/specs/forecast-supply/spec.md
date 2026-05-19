<!-- graphify-metadata
community: 41 (forecast-pipeline)
god_nodes: IForecastUpdateService, ForecastUpdateService, IMetYrMapping, MetYrMapping
bridge_nodes: ForecastCacheService (also in community 20)
related_tables: forecast_cache, all_paragliding_locations
-->

# Forecast Supply

## Purpose
Maintain fresh forecast data for paragliding locations by fetching provider forecasts, normalizing them, and persisting them into the forecast cache.

## Requirements

### Requirement: Refresh Missing Or Oldest Forecasts
The forecast update workflow SHALL prioritize paragliding locations that are missing forecast data or have the oldest available forecast data.

#### Scenario: A location has no cached forecast
Given an active paragliding location without forecast rows
When the forecast update workflow runs
Then that location SHALL be selected for provider fetch work before the cache is considered complete

#### Scenario: A location has the oldest cached forecast
Given multiple active paragliding locations with cached forecasts
When the forecast update workflow chooses the next batch to refresh
Then locations with the oldest forecast freshness SHALL be eligible for refresh first

### Requirement: Remove Stale Forecast Rows Before Refresh
Forecast data older than the current freshness window MUST be removed before fresh provider data is written.

#### Scenario: Stale forecast rows exist
Given cached forecast rows older than the cleanup threshold
When the forecast update workflow begins a refresh cycle
Then the stale rows MUST be removed before new forecast rows are persisted

### Requirement: Persist Normalized Forecast Rows In Batches
Forecast provider payloads SHALL be mapped into normalized forecast-cache rows and written in bounded batches through the forecast cache service.

> **Design rationale:** Batch-oriented writes are used to stay within provider request limits and database parameter limits while keeping forecast refresh cycles predictable.

#### Scenario: The provider returns hourly forecast entries
Given a provider response containing forecast data for a selected location batch
When the workflow maps the provider payload into forecast-cache rows
Then the normalized rows SHALL be upserted in bounded persistence batches rather than one row at a time