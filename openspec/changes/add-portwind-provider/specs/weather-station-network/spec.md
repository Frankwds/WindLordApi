## MODIFIED Requirements

### Requirement: Upsert Station Metadata Before Writing New Observations
Weather-station metadata MUST be written before new observation rows are stored for newly discovered provider stations, including PortWind stations discovered from the PortWind station-list payload.

#### Scenario: A PortWind station list contains a previously unknown station with observations
- **WHEN** the PortWind workflow discovers a station id from the station-list payload that is not yet present in the database
- **THEN** the WeatherStation record MUST be upserted before related PortWind observation rows are written

### Requirement: Preserve Provider Identity For Station Matching
Each weather station SHALL retain its provider identity while WindLordApi continues to match repeated syncs by the repository's globally unique `station_id`, including PortWind station ids taken from the top-level keys of the PortWind station-list object.

#### Scenario: The same PortWind station is seen on a later sync
- **WHEN** a later PortWind station-list refresh returns the same top-level station id
- **THEN** the existing WeatherStation record SHALL be updated instead of creating a duplicate station entry

## ADDED Requirements

### Requirement: Parse PortWind Station Metadata From The JavaScript Station List
The PortWind station-network workflow MUST extract station metadata from the object assigned to `window.stations` without executing the remaining JavaScript in the station-list file.

#### Scenario: The station list contains extra JavaScript after the station object
- **WHEN** the worker downloads the PortWind station-list source
- **THEN** it MUST deserialize only the `window.stations` assignment into provider station models and ignore trailing JavaScript

#### Scenario: The station list uses JavaScript object literal syntax
- **WHEN** the PortWind station-list payload contains unquoted property names inside the station object
- **THEN** the workflow MUST still parse the payload into PortWind station metadata without requiring strict JSON input

### Requirement: Normalize PortWind Station Labels Before Persistence
The PortWind station-network workflow MUST repair known mojibake sequences in station labels before WeatherStation metadata is persisted.

#### Scenario: A PortWind label contains mojibake text
- **WHEN** the station label contains text such as `HonningsvÃ¥g`, `TromsÃ¸`, `BodÃ¸`, or `Ã˜rnes`
- **THEN** the workflow MUST normalize the label to its intended UTF-8 representation before persisting the WeatherStation metadata

### Requirement: Derive PortWind Activity From Station List Membership And Provider Activity Flags
The PortWind station-network workflow MUST derive WeatherStation activity from membership in the latest successfully parsed PortWind station list and from the PortWind `status` and `history` booleans for each station.

#### Scenario: A PortWind station disappears from the latest station list
- **WHEN** a previously known PortWind station is missing from the latest successfully parsed PortWind station list
- **THEN** the worker MUST persist that WeatherStation as inactive

#### Scenario: A previously inactive PortWind station returns to the station list
- **WHEN** an inactive PortWind station appears again in the latest successfully parsed PortWind station list
- **THEN** the worker MUST reactivate the existing WeatherStation record instead of creating a new station

#### Scenario: A PortWind station is present and both activity booleans are true
- **WHEN** the PortWind station metadata includes `status=true` and `history=true`
- **THEN** the worker MUST persist that WeatherStation as active

#### Scenario: A PortWind station is present but one activity boolean is false
- **WHEN** the PortWind station metadata includes `status=false` or `history=false`
- **THEN** the worker MUST persist that WeatherStation as inactive even though the station remains in the latest station list

### Requirement: Fail PortWind Station Refresh On Incomplete Station-List Parsing
The PortWind station-network workflow MUST fail the entire PortWind Station Refresh when the `window.stations` assignment cannot be extracted into a complete set of provider stations.

#### Scenario: The station list cannot be fully parsed
- **WHEN** the worker cannot safely extract a complete PortWind station set from the station-list payload
- **THEN** it MUST abort the PortWind Station Refresh without applying partial activity changes