# Runtime Bootstrap And Health

## Purpose
This capability owns the worker host bootstrap that happens before recurring jobs begin. It covers host configuration, connection-string selection, integration and service registration, startup validation, the schema-contract validation pass, and the startup health-check pass. The primary implementation anchors are `src/WindLordApi.Worker/Program.cs`, `src/WindLordApi.Data/Extensions/ConfigurationExtensions.cs`, and the health-check classes under `src/WindLordApi.Worker/Startup/`.

This capability does not own startup job ordering or recurring cron execution. Those behaviors belong to separate worker orchestration capabilities.
## Requirements
### Requirement: Host bootstrap SHALL compose the worker runtime before the background service starts
The system SHALL configure logging, configuration-backed dependencies, data services, integration clients, startup health checks, and the hosted worker before calling `RunAsync` on the host.

#### Scenario: Bootstrap registers the worker runtime
- **GIVEN** the worker process starts through `Program.cs`
- **WHEN** the host is built
- **THEN** Serilog is configured for console and rolling file output
- **AND** the application registers `ApplicationDbContext`, unit of work, data services, worker services, schedulers, integration clients, and health checks in dependency injection
- **AND** the host registers `Worker` as the hosted service that will run after bootstrap completes

### Requirement: Runtime configuration SHALL resolve secrets and the database connection string by environment
The system SHALL explicitly load user secrets during bootstrap and SHALL choose `SUPABASE_CONNECTION_STRING_PRODUCTION` only for the Production environment; all other environments SHALL use `SUPABASE_CONNECTION_STRING`.

If the selected connection string is missing when `ApplicationDbContext` is resolved, startup SHALL fail instead of silently continuing.

#### Scenario: Non-production bootstrap selects the default Supabase connection string
- **GIVEN** the host environment is not Production
- **WHEN** `ApplicationDbContext` is created during bootstrap
- **THEN** configuration resolves the `SUPABASE_CONNECTION_STRING` connection string

#### Scenario: Production bootstrap selects the production Supabase connection string
- **GIVEN** the host environment is Production
- **WHEN** `ApplicationDbContext` is created during bootstrap
- **THEN** configuration resolves the `SUPABASE_CONNECTION_STRING_PRODUCTION` connection string

#### Scenario: Missing selected connection string stops startup
- **GIVEN** the selected Supabase connection string is absent or blank
- **WHEN** bootstrap resolves `ApplicationDbContext`
- **THEN** configuration throws an `InvalidOperationException`
- **AND** the outer program startup path logs fatal termination instead of continuing into worker execution

### Requirement: Startup validation SHALL fail fast for validated integration options
The system SHALL validate configured options at startup for MetFrost, Holfuy, MetYr, Open-Meteo, PortWind, and Google Geocoding registrations.

This capability SHOULD treat WindsMobi differently because the current registration does not define startup option validation for that provider.

#### Scenario: Required provider configuration is validated before the worker starts
- **GIVEN** startup registers the MetFrost, Holfuy, MetYr, Open-Meteo, PortWind, and Google Geocoding integrations
- **WHEN** the host starts
- **THEN** their bound options are validated using the configured startup validation path
- **AND** invalid required configuration prevents normal host startup

### Requirement: Schema-contract diagnostics SHALL gate startup after database resolution succeeds
The system SHALL evaluate schema-contract health checks after `ApplicationDbContext` has been resolved and before entering `host.RunAsync`.

The schema-contract validation SHALL confirm that the live database still satisfies the mapped contract the worker depends on, starting with the `forecast_cache` and `station_data` tables. Missing tables, missing mapped columns, incompatible mapped column types, incompatible nullability or exact column shape, or missing critical constraints SHALL be reported as unhealthy health-check results rather than through EF Core migration status.

#### Scenario: Forecast-cache contract mismatches are reported through startup health checks
- **GIVEN** `ApplicationDbContext` resolves successfully
- **AND** the live database no longer satisfies the mapped `forecast_cache` contract
- **WHEN** the startup health-check runner executes
- **THEN** the `forecast-cache-schema` health check reports an unhealthy result describing the mismatch
- **AND** bootstrap still proceeds to the remaining startup health checks
- **AND** startup aborts before `host.RunAsync`

#### Scenario: Forecast-cache contract matches are reported as healthy
- **GIVEN** `ApplicationDbContext` resolves successfully
- **AND** the live database satisfies the mapped `forecast_cache` contract
- **WHEN** the startup health-check runner executes
- **THEN** the `forecast-cache-schema` health check reports a healthy result
- **AND** bootstrap still proceeds to `host.RunAsync`

#### Scenario: Station-data contract mismatches abort startup
- **GIVEN** `ApplicationDbContext` resolves successfully
- **AND** the live database no longer satisfies the mapped `station_data` contract
- **WHEN** the startup health-check runner executes
- **THEN** the `station-data-schema` health check reports an unhealthy result describing the mismatch
- **AND** startup aborts before `host.RunAsync`

### Requirement: Startup health checks SHALL report dependency status without gating worker execution
The system SHALL run the registered startup health checks after database resolution and before `host.RunAsync`. The current startup health-check set SHALL include database, forecast-cache schema contract, station-data schema contract, MetFrost, Holfuy, MetYr, Open-Meteo, and PortWind.

The health-check runner SHALL log the overall report status and each individual check result. Unhealthy or degraded results SHALL be logged. Schema-tagged `Unhealthy` results SHALL stop worker startup; non-schema startup health-check failures SHALL remain advisory.

This capability MAY omit dependencies from the startup health-check set; in the current implementation, WindsMobi and Google Geocoding are registered integrations but are not included in the startup health-check pass.

#### Scenario: Startup health checks log an overall report and per-check results
- **GIVEN** the host has been built and the schema-contract diagnostics have completed
- **WHEN** the startup health-check runner executes
- **THEN** it evaluates all registered checks through `HealthCheckService`
- **AND** it logs the overall status, total check count, and total duration
- **AND** it logs each individual check as information, warning, or error based on the check result

#### Scenario: Non-schema unhealthy startup checks do not prevent worker startup
- **GIVEN** one or more registered non-schema startup health checks return `Unhealthy`
- **WHEN** the startup health-check runner completes
- **THEN** bootstrap logs those failures
- **AND** the program still proceeds to `host.RunAsync` unless a separate fatal bootstrap exception has already occurred

#### Scenario: Schema unhealthy startup checks prevent worker startup
- **GIVEN** one or more registered schema-tagged startup health checks return `Unhealthy`
- **WHEN** the startup health-check runner completes
- **THEN** bootstrap logs those failures
- **AND** the program throws a fatal startup exception before entering `host.RunAsync`

### Requirement: Open-Meteo startup health reporting SHALL use the same advisory pattern as other integrations
The system SHALL include an Open-Meteo-specific startup health check in the advisory startup health-check pass.

That check SHALL verify forecast endpoint accessibility using a deterministic request shape without changing the non-blocking behavior of the startup health-check runner.

#### Scenario: Open-Meteo startup health is reported with the other dependencies
- **GIVEN** the worker host has built the registered health checks
- **WHEN** the startup health-check runner executes before `host.RunAsync`
- **THEN** the Open-Meteo health check is included in the report alongside the other registered startup checks
- **AND** its result is logged as part of the overall dependency report

