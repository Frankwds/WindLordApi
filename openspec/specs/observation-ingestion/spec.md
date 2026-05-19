<!-- graphify-metadata
community: 52 (metfrost-sync) + 84 (holfuy-integration) + 34 (sync-contracts)
god_nodes: MetFrostSyncService, HolfuyClient, IWindsMobiSyncService, ILatestStationDataService
bridge_nodes: IStationDataService, ILatestStationDataService
related_tables: station_data, latest_station_data
-->

# Observation Ingestion

## Purpose
Normalize provider-specific weather observations into shared station-data records and maintain a current latest-observation snapshot for each station.

## Requirements

### Requirement: Map Provider Payloads Into Normalized Observation Rows
Observation sync workflows SHALL convert provider-specific payloads into normalized station-data rows before persistence.

#### Scenario: A provider returns observation values in its own schema
Given provider-specific observation payloads from MetFrost, Holfuy, or WindsMobi
When the worker processes the payloads through the relevant mapping service
Then the resulting rows SHALL be written as normalized station-data records

### Requirement: Derive Latest Station Data From Observation History
The latest-station-data table MUST be derived from normalized station-data records rather than treated as an independent source of truth.

#### Scenario: New station-data rows are persisted
Given newly stored station-data rows for one or more stations
When the latest-station-data workflow updates the current snapshot
Then the latest rows MUST be derived from the stored observation records and upserted separately

### Requirement: Ingest Observations In Provider-Sized Segments
Observation ingestion SHALL process provider data in bounded batches or provider-sized segments instead of attempting a single unbounded import.

> **Design rationale:** Provider-specific limits and large observation volumes make segmented ingestion safer for both remote APIs and database writes.

#### Scenario: A provider has many stations or records to import
Given a sync cycle with a large observation payload
When the worker persists normalized station-data rows
Then the workflow SHOULD divide the work into bounded segments that match the provider or database limits in code