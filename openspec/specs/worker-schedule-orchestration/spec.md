# Worker Schedule Orchestration

## Purpose
This capability owns the worker's recurring background schedule after one-time startup jobs finish. It defines the current UTC cron cadence in code, launches one long-running scheduler loop per workflow, and preserves the operational rules that keep recurring syncs observable and isolated when one scheduled run fails.

## Requirements

### Requirement: Recurring job cadence SHALL be defined in the worker host
The system SHALL define recurring workflow cadence in `src/WindLordApi.Worker/Worker.cs` using six-field UTC cron expressions with seconds support. The current schedule SHALL remain staggered so recurring workflows with similar cadence do not all begin at the same instant, and the weekly maintenance workflows SHALL remain in a Sunday UTC maintenance window.

Forecast refresh SHALL be scheduled as two separate recurring workflows: an authoritative MetYr refresh every 5 minutes and an Open-Meteo supplement refresh every 10 minutes.

#### Scenario: Staggered recurring jobs are launched after startup jobs complete
- **GIVEN** the worker host enters `ExecuteAsync`
- **WHEN** it finishes `StartupJobs.RunStartupJobsAsync`
- **THEN** it SHALL start recurring scheduler loops for these workflows using the configured cron expressions:
- **THEN** `SyncWindsMobiDataAsync` runs on `0 */5 * * * *`
- **THEN** `RefreshMetYrForecastsAsync` runs on `0 1/5 * * * *`
- **THEN** `SupplementOpenMeteoForecastsAsync` runs on `0 6/10 * * * *`
- **THEN** `SyncLatestStationDataAsync` for MetFrost runs on `0 2/5 * * * *`
- **THEN** `SyncPortWindLatestStationDataAsync` runs on `0 3 * * * *`
- **THEN** `SyncHolfuyDataAsync` runs on `30 */15 * * * *`
- **THEN** the Sunday maintenance workflows run on `0 0 3 * * SUN`, `0 0 4 * * SUN`, `0 0 5 * * SUN`, and `0 0 6 * * SUN`

### Requirement: Forecast providers SHALL be scheduled through independent recurring loops
The worker SHALL launch the authoritative MetYr forecast refresh workflow and the Open-Meteo supplement workflow as separate recurring scheduler loops after startup jobs complete.

Failure or cancellation of one forecast workflow invocation SHALL not stop the other workflow from remaining scheduled for its later occurrences.

#### Scenario: Open-Meteo workflow failure does not remove MetYr from the schedule
- **GIVEN** the Open-Meteo supplement scheduler loop is already running
- **AND** one Open-Meteo invocation throws after the loop has started
- **WHEN** the worker continues running
- **THEN** the Open-Meteo scheduler loop remains eligible for its next occurrence
- **AND** the authoritative MetYr forecast refresh workflow remains scheduled independently for its own next occurrence

### Requirement: Each scheduled execution SHALL run in an isolated scoped invocation
The scheduler SHALL parse each cron expression with `CronFormat.IncludeSeconds`, calculate the next occurrence in UTC, wait until that time, create a fresh DI scope for the scheduled run, resolve the workflow service for that scheduler instance, and invoke the supplied job action.

#### Scenario: A scheduled job executes through a fresh scope
- **GIVEN** a recurring job has a valid cron expression and a next UTC occurrence
- **WHEN** `CronScheduler<TService>.RunAsync` reaches that occurrence
- **THEN** it SHALL create a new service scope for that run
- **THEN** it SHALL resolve `TService` from that scope
- **THEN** it SHALL execute the provided job action with the host cancellation token

### Requirement: Scheduler failures SHALL not stop later occurrences of the same job
The system SHALL log invalid cron configuration, missing next occurrences, job start, job completion, cancellation, and per-run failures. If a scheduled run throws after the scheduler loop has started, the scheduler SHALL log the error and continue waiting for the next occurrence instead of terminating the loop.

#### Scenario: A scheduled run fails after the scheduler loop has started
- **GIVEN** a recurring scheduler loop is already running for a workflow
- **WHEN** a single invocation throws during `ExecuteJobOnceAsync`
- **THEN** the scheduler SHALL log `Error in scheduled job`
- **THEN** it SHALL keep the loop alive
- **THEN** the same workflow SHALL be eligible to run again at its next cron occurrence

#### Scenario: An invalid cron expression is supplied to a scheduler loop
- **GIVEN** `CronScheduler<TService>.RunAsync` receives an invalid cron expression
- **WHEN** it attempts to parse the expression with seconds support
- **THEN** it SHALL log `Invalid cron expression`
- **THEN** it SHALL throw instead of silently falling back to another schedule

### Requirement: Schedule visibility SHALL be emitted at worker startup
The worker SHALL calculate the next run time for each recurring workflow before starting the scheduler loops and log a schedule overview that includes the job name, cron expression, expected duration, and first planned UTC run time.

#### Scenario: The worker logs the current recurring schedule
- **GIVEN** the worker has computed the next UTC occurrence for each recurring workflow
- **WHEN** it calls `PrintJobSchedule`
- **THEN** it SHALL log a startup overview ordered by first run time
- **THEN** each entry SHALL include the workflow name, cron expression, expected duration, and first run in UTC