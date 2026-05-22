# Worker Schedule Orchestration

## Purpose
This capability owns the worker's recurring background schedule after one-time startup jobs finish. It defines the current UTC cron cadence in code, launches one long-running scheduler loop per workflow, and preserves the operational rules that keep recurring syncs observable and isolated when one scheduled run fails.

## Requirements

### Requirement: Recurring job cadence SHALL be defined in the worker host
The system SHALL define recurring workflow cadence in `src/WindLordApi.Worker/Worker.cs` using six-field UTC cron expressions with seconds support. The current schedule SHALL remain staggered so the five-minute workflows do not all begin at the same instant, and the weekly maintenance workflows SHALL remain in a Sunday UTC maintenance window.

#### Scenario: Staggered recurring jobs are launched after startup jobs complete
- **GIVEN** the worker host enters `ExecuteAsync`
- **WHEN** it finishes `StartupJobs.RunStartupJobsAsync`
- **THEN** it SHALL start recurring scheduler loops for these workflows using the configured cron expressions:
- **THEN** `SyncWindsMobiDataAsync` runs on `0 */5 * * * *`
- **THEN** `UpdateForecastsAsync` runs on `0 1/5 * * * *`
- **THEN** `SyncLatestStationDataAsync` for MetFrost runs on `0 2/5 * * * *`
- **THEN** `SyncPortWindLatestStationDataAsync` runs on `0 3 * * * *`
- **THEN** `SyncHolfuyDataAsync` runs on `30 */15 * * * *`
- **THEN** the Sunday maintenance workflows run on `0 0 3 * * SUN`, `0 0 4 * * SUN`, `0 0 5 * * SUN`, and `0 0 6 * * SUN`

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