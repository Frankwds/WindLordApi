# Runtime Bootstrap And Health

## Purpose
This capability owns the worker host bootstrap that happens before recurring jobs begin. It covers host configuration, connection-string selection, integration and service registration, startup validation, the pending-migration diagnostic, and the startup health-check pass. The primary implementation anchors are `src/WindLordApi.Worker/Program.cs`, `src/WindLordApi.Data/Extensions/ConfigurationExtensions.cs`, and the health-check classes under `src/WindLordApi.Worker/Startup/`.

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
The system SHALL validate configured options at startup for MetFrost, Holfuy, MetYr, PortWind, and Google Geocoding registrations.

This capability SHOULD treat WindsMobi differently because the current registration does not define startup option validation for that provider.

#### Scenario: Required provider configuration is validated before the worker starts
- **GIVEN** startup registers the MetFrost, Holfuy, MetYr, PortWind, and Google Geocoding integrations
- **WHEN** the host starts
- **THEN** their bound options are validated using the configured startup validation path
- **AND** invalid required configuration prevents normal host startup

### Requirement: Pending-migration diagnostics SHALL be advisory after database resolution succeeds
The system SHALL attempt to check for pending EF Core migrations before running startup health checks and before entering `host.RunAsync`.

If the migration query reports pending migrations, the system SHALL log the migration names and an update command. If the migration query itself fails after `ApplicationDbContext` has been resolved, the system SHALL log the failure and continue startup.

#### Scenario: Pending migrations are reported without blocking startup
- **GIVEN** `ApplicationDbContext` resolves successfully
- **AND** the database reports one or more pending migrations
- **WHEN** the pending-migration check runs
- **THEN** startup logs the pending migration names and the recommended `dotnet ef database update` command
- **AND** bootstrap continues to the startup health-check pass

#### Scenario: Migration query failure is logged and startup continues
- **GIVEN** `ApplicationDbContext` resolves successfully
- **AND** the migration query throws while checking database state
- **WHEN** the pending-migration check runs
- **THEN** startup logs the exception as a migration-check failure
- **AND** bootstrap still proceeds to startup health checks

### Requirement: Startup health checks SHALL report dependency status without gating worker execution
The system SHALL run the registered startup health checks after the migration diagnostic and before `host.RunAsync`. The current startup health-check set SHALL include database, MetFrost, Holfuy, MetYr, and PortWind.

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