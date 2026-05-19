## MODIFIED Requirements

### Requirement: Map Provider Payloads Into Normalized Observation Rows
Observation sync workflows SHALL convert provider-specific payloads from every supported station-data Provider, including PortWind, into normalized station-data rows before persistence.

#### Scenario: PortWind returns observation values in its own schema
- **WHEN** the worker processes PortWind `latestandprevious` payloads through the PortWind mapping service
- **THEN** the resulting rows SHALL be written as normalized StationData records

### Requirement: Ingest Observations In Provider-Sized Segments
Observation ingestion SHALL process provider data in bounded batches or provider-sized segments instead of attempting a single unbounded import, including Providers such as PortWind that require one request per station id.

#### Scenario: A PortWind sync cycle needs many station requests
- **WHEN** the worker fetches PortWind observations for a large set of station ids
- **THEN** the workflow SHALL divide the requests into bounded provider-sized segments rather than issuing one unbounded import pass

## ADDED Requirements

### Requirement: Fetch PortWind Observations Per Station Identifier
The PortWind observation workflow MUST call the PortWind observation endpoint separately for each active PortWind station id already stored in the database using the `latestandprevious` dataset.

#### Scenario: The PortWind workflow starts observation ingestion
- **WHEN** the worker prepares observation requests for PortWind stations
- **THEN** it MUST read active PortWind station ids from the database and build each request with the target station id and `dataset=latestandprevious`

### Requirement: Normalize PortWind Epoch Millisecond Timestamps
The PortWind observation workflow MUST interpret `data[].uts` as the normalized PortWind observation timestamp before mapping StationData rows.

#### Scenario: A PortWind observation payload contains integer timestamps
- **WHEN** the payload includes `data[].uts`
- **THEN** the workflow MUST convert that value from epoch milliseconds into the application's normalized timestamp representation before persistence

### Requirement: Ignore PortWind Comparative Helper Fields As Independent Observations
The PortWind observation workflow MUST map the current measurement values into StationData rows without treating `*_previous` helper fields as separate observations.

#### Scenario: PortWind returns comparison fields with the latest row
- **WHEN** the `latestandprevious` payload includes values such as `temperature_avg_previous`, `wind_speed_avg_previous`, or `pressure_avg_previous`
- **THEN** the worker MUST NOT persist those helper fields as independent StationData rows

### Requirement: Map PortWind Average Temperature Into The Normalized Temperature Field
The PortWind observation workflow MUST use `temperature_avg` as the normalized StationData temperature when that value is present.

#### Scenario: PortWind returns temperature minimum, average, and maximum values
- **WHEN** the PortWind payload includes `temperature_min`, `temperature_avg`, and `temperature_max`
- **THEN** the worker MUST map `temperature_avg` into the normalized StationData temperature field

### Requirement: Continue PortWind Observation Sync On Per-Station Failures
The PortWind observation workflow MUST continue processing remaining stations when an individual PortWind station request fails.

#### Scenario: One PortWind station request fails during a larger sync
- **WHEN** a PortWind observation request for one active station fails
- **THEN** the worker MUST continue processing the remaining active PortWind stations instead of aborting the whole PortWind Observation Sync

#### Scenario: A PortWind station returns an empty observation array
- **WHEN** the PortWind observation payload contains an empty `data` array for an active station
- **THEN** the worker MUST leave that WeatherStation active and skip observation persistence for that response