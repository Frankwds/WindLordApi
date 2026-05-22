## MODIFIED Requirements

### Requirement: Startup validation SHALL fail fast for validated integration options
The system SHALL validate configured options at startup for MetFrost, Holfuy, MetYr, Open-Meteo, PortWind, and Google Geocoding registrations.

This capability SHOULD treat WindsMobi differently because the current registration does not define startup option validation for that provider.

#### Scenario: Required provider configuration is validated before the worker starts
- **GIVEN** startup registers the MetFrost, Holfuy, MetYr, Open-Meteo, PortWind, and Google Geocoding integrations
- **WHEN** the host starts
- **THEN** their bound options are validated using the configured startup validation path
- **AND** invalid required configuration prevents normal host startup

### Requirement: Startup health checks SHALL report dependency status without gating worker execution
The system SHALL run the registered startup health checks after the migration diagnostic and before `host.RunAsync`. The current startup health-check set SHALL include database, MetFrost, Holfuy, MetYr, Open-Meteo, and PortWind.

The health-check runner SHALL log the overall report status and each individual check result. Unhealthy or degraded results SHALL be logged, but SHALL NOT by themselves stop worker startup.

This capability MAY omit dependencies from the startup health-check set; in the current implementation, WindsMobi and Google Geocoding are registered integrations but are not included in the startup health-check pass.

#### Scenario: Startup health checks log an overall report and per-check results
- **GIVEN** the host has been built and the migration diagnostic has completed
- **WHEN** the startup health-check runner executes
- **THEN** it evaluates all registered checks through `HealthCheckService`
- **AND** it logs the overall status, total check count, and total duration
- **AND** it logs each individual check as information, warning, or error based on the check result

#### Scenario: Unhealthy startup checks do not prevent worker startup
- **GIVEN** one or more registered startup health checks return `Unhealthy`
- **WHEN** the startup health-check runner completes
- **THEN** bootstrap logs those failures
- **AND** the program still proceeds to `host.RunAsync` unless a separate fatal bootstrap exception has already occurred

## ADDED Requirements

### Requirement: Open-Meteo startup health reporting SHALL use the same advisory pattern as other integrations
The system SHALL include an Open-Meteo-specific startup health check in the advisory startup health-check pass.

That check SHALL verify forecast endpoint accessibility using a deterministic request shape without changing the non-blocking behavior of the startup health-check runner.

#### Scenario: Open-Meteo startup health is reported with the other dependencies
- **GIVEN** the worker host has built the registered health checks
- **WHEN** the startup health-check runner executes before `host.RunAsync`
- **THEN** the Open-Meteo health check is included in the report alongside the other registered startup checks
- **AND** its result is logged as part of the overall dependency report