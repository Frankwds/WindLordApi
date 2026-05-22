## MODIFIED Requirements

### Requirement: Forecast cache rows are unique by location and forecast time
The system SHALL identify forecast cache rows by the pair of paragliding location and forecast time.

When a write targets an existing `(LocationId, Time)` pair, the system SHALL preserve exactly one row for that key and SHALL apply the provider-precedence rule before deciding whether the mutable forecast payload is updated.

The current upsert projection SHOULD preserve the existing row identity and creation timestamp because it updates forecast payload fields and `UpdatedAt`, but does not replace `Id`, `LocationId`, `Time`, or `CreatedAt`.

#### Scenario: Conflicting Open-Meteo writes do not duplicate a Yr-backed row
- **GIVEN** `forecast_cache` already contains a Yr-backed row for a paragliding location at a specific forecast time
- **WHEN** the repository upserts an Open-Meteo-backed row for the same `LocationId` and `Time`
- **THEN** exactly one row SHALL remain for that key
- **AND** the stored row SHALL remain Yr-backed

#### Scenario: Same forecast time across different locations does not conflict
- **GIVEN** two paragliding locations share the same forecast timestamp
- **WHEN** the repository upserts one forecast row for each location at that time
- **THEN** both rows SHALL be stored because the uniqueness rule is scoped to `(LocationId, Time)`

### Requirement: Matching forecast rows replace the mutable forecast payload
For an existing `(LocationId, Time)` row, the repository SHALL replace the stored mutable forecast payload with the incoming payload during upsert unless the existing row is Yr-backed and the incoming row is Open-Meteo-backed.

An incoming Yr-backed row SHALL be allowed to replace the mutable payload of an existing Open-Meteo-backed row for the same key.

The mutable payload currently includes surface conditions, landing conditions, atmospheric fields, precipitation bounds, `IsYrData`, and `UpdatedAt`.

Database constraints SHALL continue to enforce the shared persistence contract for these rows, including the `(LocationId, Time)` alternate key and the rule that `IsDay` can only be `0` or `1`.

#### Scenario: Existing Yr-backed rows reject conflicting Open-Meteo payloads
- **GIVEN** a stored Yr-backed forecast row already exists for a location and forecast time
- **WHEN** an incoming Open-Meteo-backed row is upserted for that same key
- **THEN** the stored forecast payload SHALL remain unchanged
- **AND** the write SHALL not create a second row for that key

#### Scenario: Later Yr-backed rows replace conflicting Open-Meteo payloads
- **GIVEN** a stored Open-Meteo-backed forecast row already exists for a location and forecast time
- **WHEN** an incoming Yr-backed row is upserted for that same key
- **THEN** the stored forecast payload SHALL be updated with the Yr-backed payload
- **AND** the resulting row SHALL be marked as Yr-backed