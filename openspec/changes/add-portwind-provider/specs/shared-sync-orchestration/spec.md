## MODIFIED Requirements

### Requirement: Keep Sync Responsibilities Separate
The worker SHALL schedule and execute forecast, provider sync, and location-enrichment workflows as separate responsibilities rather than collapsing them into a single all-purpose job, and the PortWind Provider SHALL run through separate workflow-specific service registrations for PortWind Station Refresh and PortWind Observation Sync.

#### Scenario: The worker starts recurring jobs with PortWind enabled
- **WHEN** the worker host has started and registered its scheduled services
- **THEN** the PortWind station refresh and PortWind observation sync SHALL each run through their own provider-specific service instead of being merged into another Provider workflow

### Requirement: Execute Syncs With Validated Provider Configuration
Each provider-backed sync MUST run through a registered client and validated options object before external calls are attempted, including the PortWind Provider.

#### Scenario: A PortWind sync service is resolved from dependency injection
- **WHEN** either PortWind workflow is about to use its external client configuration
- **THEN** the workflow MUST rely on PortWind's configured options and registered client abstraction before making remote calls

## ADDED Requirements

### Requirement: Schedule PortWind Station Refresh Less Frequently Than PortWind Observation Sync
The worker MUST schedule PortWind Station Refresh on a lower cadence than PortWind Observation Sync, with the station refresh running weekly.

#### Scenario: PortWind recurring jobs are configured
- **WHEN** the worker starts PortWind recurring jobs
- **THEN** PortWind Station Refresh MUST run weekly and PortWind Observation Sync MUST run on its own more frequent cadence