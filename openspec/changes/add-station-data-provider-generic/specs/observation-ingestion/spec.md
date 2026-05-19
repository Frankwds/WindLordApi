## MODIFIED Requirements

### Requirement: Map Provider Payloads Into Normalized Observation Rows
Observation sync workflows SHALL convert provider-specific payloads from every supported station-data Provider into normalized station-data rows before persistence.

#### Scenario: A supported Provider returns observation values in its own schema
- **WHEN** the worker processes provider-specific observation payloads through the relevant mapping service
- **THEN** the resulting rows SHALL be written as normalized station-data records

### Requirement: Ingest Observations In Provider-Sized Segments
Observation ingestion SHALL process provider data in bounded batches or provider-sized segments instead of attempting a single unbounded import, including when a newly onboarded Provider needs a different segment size from existing Providers.

#### Scenario: A newly onboarded Provider has many stations or records to import
- **WHEN** the worker persists normalized station-data rows from a large sync cycle
- **THEN** the workflow SHALL divide the work into bounded segments that match the Provider or database limits in code