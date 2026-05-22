# Open-Meteo Forecast Supplement

## Purpose
This capability documents the Open-Meteo workflow that supplements takeoff forecast coverage for paragliding locations. The current entry points are the startup job runner in `StartupJobs.RunStartupJobsAsync` and the recurring worker invocation scheduled from `Worker.ExecuteAsync`. This workflow owns independent batched Open-Meteo requests, shortest-tail candidate selection, request-order correlation, and normalization of Open-Meteo rows before they are persisted through the shared forecast-cache persistence contract. Shared persistence rules such as composite-key upsert semantics and Yr precedence belong to the separate forecast-cache lifecycle capability.

Primary implementation surfaces today are `OpenMeteoForecastSupplementService`, `IParaglidingLocationService`, `IForecastCacheService`, and the Open-Meteo client and mapping services.

## Requirements

### Requirement: Open-Meteo SHALL run as a separate provider-owned workflow
The system SHALL run Open-Meteo forecast supplementation as a workflow separate from the authoritative MetYr refresh workflow.

The Open-Meteo supplement workflow SHALL issue one batched takeoff-forecast request for the selected locations in that workflow run, using takeoff coordinates truncated to three decimals and a request horizon from 48 hours to 96 hours after the request time, map the returned rows as Open-Meteo-backed forecast entries, and persist them only through the shared forecast-cache repository contract.

Open-Meteo-supplemented rows SHALL set `IsYrData = false`, SHALL populate only the currently persisted takeoff surface fields other than wind gusts, SHALL round mapped numeric values to match destination forecast-cache precision, and SHALL leave landing forecast fields plus any unsupported or unavailable fields unset in this capability.

#### Scenario: Open-Meteo writes takeoff-only supplemental rows
- **GIVEN** the Open-Meteo supplement workflow selects active main paragliding locations for a batch
- **WHEN** it persists mapped Open-Meteo forecast rows for those locations
- **THEN** the persisted rows are marked with `IsYrData = false`
- **AND** landing forecast fields remain unset for those rows

### Requirement: Open-Meteo supplement selection SHALL prioritize the shortest forecast tail
The Open-Meteo supplement workflow SHALL choose candidate locations through one ordering that uses the latest Open-Meteo-backed forecast timestamp as the tail signal.

Locations with no Open-Meteo-backed forecast rows SHALL sort ahead of locations that already have Open-Meteo-backed coverage.

When multiple eligible locations already have Open-Meteo-backed coverage, the workflow SHALL prefer the locations whose latest Open-Meteo-backed forecast timestamp is earliest.

This priority SHALL be based on Open-Meteo-backed forecast horizon rather than generic forecast freshness or `UpdatedAt` values from Yr-backed rows.

#### Scenario: Locations without Open-Meteo coverage have the shortest tail
- **GIVEN** active main paragliding locations exist
- **AND** some locations have no Open-Meteo-backed forecast rows while others have existing Open-Meteo-backed rows
- **WHEN** the Open-Meteo supplement workflow selects its next batch
- **THEN** locations with no Open-Meteo-backed forecast rows are selected before locations whose Open-Meteo-backed forecast horizon already extends into the future

#### Scenario: Shorter Open-Meteo horizons are selected before longer horizons
- **GIVEN** multiple active main paragliding locations already have Open-Meteo-backed forecast rows
- **AND** those locations have different latest Open-Meteo-backed forecast timestamps
- **WHEN** the Open-Meteo supplement workflow selects its next batch
- **THEN** the location whose latest Open-Meteo-backed forecast timestamp is earliest is selected before locations with later Open-Meteo-backed forecast timestamps

### Requirement: Open-Meteo failures stay batch-scoped
The Open-Meteo supplement workflow SHALL log batch-level request, mapping, or persistence failures once per failed run and SHALL not block later MetYr refresh runs from persisting authoritative rows on their normal cadence.

#### Scenario: Open-Meteo batch failure does not stop later MetYr refreshes
- **GIVEN** the Open-Meteo supplement workflow encounters a batch-level failure
- **WHEN** later authoritative MetYr refresh runs occur on schedule
- **THEN** those MetYr runs still fetch and persist Yr-derived forecast rows
- **AND** the Open-Meteo failure is logged without transferring authority to Open-Meteo

### Requirement: Batched Open-Meteo responses SHALL correlate predictably to selected locations
The Open-Meteo supplement workflow SHALL correlate each Open-Meteo response block back to the selected paragliding location set without relying exclusively on a provider-supplied `location_id` field.

Request order SHALL be the primary correlation key.

#### Scenario: Request order correlates batched response blocks
- **GIVEN** an Open-Meteo supplement batch contains multiple selected takeoff locations
- **AND** the workflow submits those locations to Open-Meteo in a defined request order
- **WHEN** the Open-Meteo response returns one forecast block per requested coordinate pair
- **THEN** each response block is matched back to the location at the same request position

### Requirement: Open-Meteo weather normalization SHALL map WMO codes into the existing app vocabulary
The system SHALL map Open-Meteo WMO `weather_code` values plus `is_day` into the Yr-compatible weather-code vocabulary consumed by WindLord.

The mapping SHALL use `is_day` only for WMO codes `0`, `1`, and `2`. Unknown or unsupported WMO codes SHALL remain unset rather than being coerced into an unrelated target code.

#### Scenario: Day and night variants are chosen only for supported codes
- **GIVEN** an Open-Meteo hourly row has WMO code `0`, `1`, or `2`
- **AND** the row includes an `is_day` value
- **WHEN** the row is normalized into a forecast cache entry
- **THEN** the weather code uses the matching day or night variant from the existing app vocabulary

#### Scenario: Unsupported WMO codes remain unset
- **GIVEN** an Open-Meteo hourly row has a WMO weather code that is not supported by the locked mapping table
- **WHEN** the row is normalized into a forecast cache entry
- **THEN** the stored weather code remains unset