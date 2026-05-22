# Worker Startup Sequence

## Purpose
This capability documents the one-time startup workflow that the worker runs after the host has been built and before recurring cron loops begin. It exists to front-load critical synchronization and enrichment work, preserve operational ordering where one startup job depends on data from an earlier one, and keep startup resilient by isolating failures to the job that raised them.

This capability begins in `Worker.ExecuteAsync()` when it calls `StartupJobs.RunStartupJobsAsync(...)`. It does not include host bootstrap concerns such as migration checks, connection-string selection, or startup health checks from `Program.cs`.

## Requirements

### Requirement: Startup jobs run before recurring schedules
The worker SHALL execute the startup job sequence once before it initializes recurring cron schedule state and starts any long-running scheduler loops.

#### Scenario: Worker startup runs one-time jobs first
- **GIVEN** the worker host has started and `Worker.ExecuteAsync()` begins running
- **WHEN** the worker enters its execution path
- **THEN** it invokes `StartupJobs.RunStartupJobsAsync(...)` before it calculates cron schedules or starts any `CronScheduler<TService>.RunAsync(...)` loops

### Requirement: Startup jobs follow the declared execution order
The startup runner SHALL execute startup jobs sequentially in the order declared in `StartupJobs.cs`.

The current implemented order is:
- Open-Meteo forecast supplement
- MetYr forecast refresh
- PortWind station refresh
- PortWind latest-station-data sync
- WindsMobi sync
- Country locator
- Holfuy sync
- MetFrost latest-station-data sync
- MetFrost weather-station sync
- MetFrost weather-station active-status sync

#### Scenario: Open-Meteo startup runs before MetYr startup
- **GIVEN** the worker is executing startup jobs
- **WHEN** the forecast startup workflows run
- **THEN** `IOpenMeteoForecastSupplementService.SupplementForecastsAsync(...)` runs before `IMetYrForecastRefreshService.UpdateForecastsAsync(...)`
- **AND** the later MetYr startup refresh remains able to take precedence on overlapping forecast-cache rows

#### Scenario: PortWind metadata is loaded before PortWind latest data
- **GIVEN** the worker is executing startup jobs
- **WHEN** the PortWind startup workflow runs
- **THEN** `IPortWindStationRefreshService.SyncWeatherStationsAsync(...)` runs before `IPortWindLatestDataSyncService.SyncLatestStationDataAsync(...)`
- **AND** PortWind station metadata is refreshed before dependent latest observations are synchronized

#### Scenario: Later startup jobs wait for earlier jobs to finish
- **GIVEN** the startup runner is processing its declared job list
- **WHEN** one startup job is running
- **THEN** the next startup job does not begin until the current job has completed or failed

### Requirement: Startup job failures are isolated and logged
The startup runner SHALL wrap each startup job in its own error boundary so that a failure in one job is logged and later startup jobs are still attempted.

#### Scenario: A startup job fails without aborting the sequence
- **GIVEN** one startup job throws an exception during startup
- **WHEN** the runner catches that exception
- **THEN** it logs an error message for that specific job
- **AND** it continues to attempt the remaining startup jobs in their declared order
- **AND** it logs startup completion after the sequence finishes attempting all jobs

### Requirement: Each startup job resolves services from a fresh scope
The startup runner SHALL create a new dependency-injection scope for each startup job before resolving that job's service implementation.

#### Scenario: Startup job service resolution is isolated per job
- **GIVEN** the runner is about to execute a startup job
- **WHEN** it resolves the job's service implementation
- **THEN** it creates a fresh service scope for that job
- **AND** it does not reuse the previous job's scoped service instance