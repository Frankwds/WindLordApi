## MODIFIED Requirements

### Requirement: Recurring job cadence SHALL be defined in the worker host
The system SHALL define recurring workflow cadence in `src/WindLordApi.Worker/Worker.cs` using six-field UTC cron expressions with seconds support. The current schedule SHALL remain staggered so recurring workflows with similar cadence do not all begin at the same instant, and the weekly maintenance workflows SHALL remain in a Sunday UTC maintenance window.

Forecast refresh SHALL be scheduled as two separate recurring workflows: an authoritative MetYr refresh every 5 minutes and an Open-Meteo supplement refresh every 10 minutes.

#### Scenario: Staggered recurring jobs are launched after startup jobs complete
- **GIVEN** the worker host enters `ExecuteAsync`
- **WHEN** it finishes `StartupJobs.RunStartupJobsAsync`
- **THEN** it SHALL start recurring scheduler loops for these workflows using the configured cron expressions:
- **THEN** `SyncWindsMobiDataAsync` runs on `0 */5 * * * *`
- **THEN** the authoritative MetYr forecast refresh workflow runs every 5 minutes on a staggered UTC cron expression
- **THEN** the Open-Meteo supplement refresh workflow runs every 10 minutes on a staggered UTC cron expression separate from the MetYr forecast schedule
- **THEN** `SyncLatestStationDataAsync` for MetFrost runs on `0 2/5 * * * *`
- **THEN** `SyncPortWindLatestStationDataAsync` runs on `0 3 * * * *`
- **THEN** `SyncHolfuyDataAsync` runs on `30 */15 * * * *`
- **THEN** the Sunday maintenance workflows run on `0 0 3 * * SUN`, `0 0 4 * * SUN`, `0 0 5 * * SUN`, and `0 0 6 * * SUN`

## ADDED Requirements

### Requirement: Forecast providers SHALL be scheduled through independent recurring loops
The worker SHALL launch the authoritative MetYr forecast refresh workflow and the Open-Meteo supplement workflow as separate recurring scheduler loops after startup jobs complete.

Failure or cancellation of one forecast workflow invocation SHALL not stop the other workflow from remaining scheduled for its later occurrences.

#### Scenario: Open-Meteo workflow failure does not remove MetYr from the schedule
- **GIVEN** the Open-Meteo supplement scheduler loop is already running
- **AND** one Open-Meteo invocation throws after the loop has started
- **WHEN** the worker continues running
- **THEN** the Open-Meteo scheduler loop remains eligible for its next occurrence
- **AND** the authoritative MetYr forecast refresh workflow remains scheduled independently for its own next occurrence