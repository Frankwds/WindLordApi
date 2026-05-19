## MODIFIED Requirements

### Requirement: Keep Sync Responsibilities Separate
The worker SHALL schedule and execute forecast, provider sync, and location-enrichment workflows as separate responsibilities rather than collapsing them into a single all-purpose job, and any newly added station-data Provider SHALL run through its own workflow-specific service registration.

#### Scenario: The worker starts its recurring jobs with an added station-data Provider
- **WHEN** the worker host has started and registered its scheduled services
- **THEN** each sync responsibility, including the added Provider workflow, SHALL run through its own workflow-specific service

### Requirement: Execute Syncs With Validated Provider Configuration
Each provider-backed sync MUST run through a registered client and validated options object before external calls are attempted, including any newly onboarded station-data Provider.

#### Scenario: A newly added Provider sync service is resolved from dependency injection
- **WHEN** a Provider sync service is about to execute and uses its external client configuration
- **THEN** the workflow MUST rely on the configured options and registered client abstraction for that Provider