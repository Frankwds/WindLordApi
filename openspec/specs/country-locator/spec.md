# Country Locator

## Purpose
The country-locator capability enriches weather stations whose country metadata is missing by reverse geocoding their coordinates through Google Geocoding. It exists to backfill `Country` and the worker-owned `IsMain` flag for unresolved weather stations without changing other weather-station metadata, and it runs both during startup and on the weekly maintenance schedule.

## Requirements

### Requirement: Select unresolved weather stations for enrichment
The system SHALL treat weather stations with `Country = null` or `Country = "UKJENT"` as unresolved and SHALL make them eligible for country enrichment.

#### Scenario: Missing-country stations are selected
- **GIVEN** persisted weather stations whose `Country` value is `null`, `"UKJENT"`, or a populated country name
- **WHEN** the country-locator workflow starts
- **THEN** it fetches only the stations whose `Country` is `null` or `"UKJENT"`
- **AND** it leaves already-resolved stations out of the run

#### Scenario: No unresolved stations exist
- **GIVEN** no persisted weather station has `Country = null` or `Country = "UKJENT"`
- **WHEN** the country-locator workflow starts
- **THEN** it completes without making geocoding calls or persistence updates

### Requirement: Enrich unresolved stations in rate-limited batches
The system SHALL process unresolved weather stations in batches of 40, SHALL delay one second between batches, and SHALL reverse geocode each station by latitude and longitude through Google Geocoding.

#### Scenario: Stations are processed in bounded batches
- **GIVEN** more than 40 unresolved weather stations exist
- **WHEN** the workflow runs
- **THEN** it processes at most 40 stations before persisting that batch
- **AND** it waits one second before starting the next batch

#### Scenario: A station cannot be geocoded
- **GIVEN** an unresolved weather station whose Google Geocoding lookup returns no country
- **WHEN** the workflow processes that station
- **THEN** it logs a warning for the unresolved station
- **AND** it continues processing the rest of the batch
- **AND** it leaves that station eligible for a future enrichment run

### Requirement: Persist only country enrichment fields
The system SHALL persist only `Country` and `IsMain` when applying country-enrichment results, and normal weather-station upserts SHALL NOT overwrite those fields.

#### Scenario: A country is resolved
- **GIVEN** an unresolved weather station whose reverse-geocoding lookup returns a country name
- **WHEN** the workflow persists enrichment results
- **THEN** it updates the station's `Country`
- **AND** it updates only the worker-owned `Country` and `IsMain` fields during that persistence step

#### Scenario: Normal provider syncs preserve enrichment fields
- **GIVEN** a weather station whose `Country` and `IsMain` were previously set by country enrichment
- **WHEN** a normal weather-station provider upsert runs later
- **THEN** that upsert does not overwrite `Country` or `IsMain`

### Requirement: Mark Norwegian stations as main during enrichment
The system SHALL set `IsMain = true` for weather stations whose resolved country is `Norway`.

#### Scenario: A Norwegian station is enriched
- **GIVEN** an unresolved weather station whose reverse-geocoding lookup returns `Norway`
- **WHEN** the workflow persists enrichment results
- **THEN** the station's `Country` is set to `Norway`
- **AND** the station's `IsMain` flag is set to `true`

#### Scenario: A non-Norwegian station is enriched
- **GIVEN** an unresolved weather station whose reverse-geocoding lookup returns a country other than `Norway`
- **WHEN** the workflow persists enrichment results
- **THEN** the station's `Country` is updated to the resolved country
- **AND** the workflow does not apply the special `IsMain = true` rule used for Norwegian stations

### Requirement: Participate in startup and weekly maintenance
The system SHALL run country enrichment during startup and SHALL also schedule it for Sundays at 05:00 UTC.

#### Scenario: Startup executes country enrichment once
- **GIVEN** the worker host is starting
- **WHEN** startup jobs run
- **THEN** the country-locator workflow is invoked once as part of the startup job sequence

#### Scenario: Weekly maintenance executes country enrichment
- **GIVEN** the worker host is running normally
- **WHEN** the weekly maintenance schedule reaches Sunday at 05:00 UTC
- **THEN** the scheduler invokes the country-locator workflow again