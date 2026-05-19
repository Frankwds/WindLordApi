## MODIFIED Requirements

### Requirement: Upsert Station Metadata Before Writing New Observations
Weather-station metadata MUST be written before new observation rows are stored for newly discovered provider stations, including stations introduced through a newly onboarded Provider.

#### Scenario: A newly onboarded Provider returns a previously unknown station with observations
- **WHEN** a supported Provider payload contains a station that is not yet present in the database
- **THEN** the WeatherStation record MUST be upserted before related observation rows are written

### Requirement: Preserve Provider Identity For Station Matching
Each weather station SHALL keep its provider identity and provider station identifier as the stable lookup key for repeated syncs, including stations imported from a newly added Provider.

#### Scenario: A newly onboarded Provider reports station metadata
- **WHEN** the station-network workflow upserts a WeatherStation from a supported Provider payload
- **THEN** the worker SHALL match and persist the station by the combination of provider identity and provider station identifier

#### Scenario: The same provider station is seen on a later sync
- **WHEN** a later sync returns updated metadata for an existing provider station
- **THEN** the existing WeatherStation record SHALL be updated instead of creating a duplicate station entry