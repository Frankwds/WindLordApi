## Context

WindLordApi currently refreshes forecast cache data through one combined worker service. That shape worked while Open-Meteo was only an in-memory supplement merged into the same per-location write path before persistence, because Yr precedence was preserved implicitly by the merge order. The next change breaks that assumption: MetYr and Open-Meteo need different schedules, different location-selection priorities, and different operational ownership.

The repository contract is the seam that matters most. Forecast cache rows remain unique by `(LocationId, Time)`, and the current upsert implementation updates matching rows with incoming values unconditionally. That is acceptable in the current combined workflow because overlapping Open-Meteo rows are filtered out before the upsert. It is not acceptable once MetYr refresh and Open-Meteo supplementation become separate scheduled workflows, because a later Open-Meteo write must never overwrite a higher-quality Yr-backed row.

This design therefore splits orchestration by provider and moves precedence enforcement into the forecast-cache persistence contract. MetYr becomes the authoritative forecast refresh workflow, Open-Meteo becomes a lower-frequency supplement workflow, and the forecast-cache repository becomes responsible for preserving the invariant that Yr wins any overlapping timestamp.

## Goals / Non-Goals

**Goals:**
- Replace the single combined forecast refresh workflow with two explicitly named worker services: one for authoritative MetYr refresh and one for Open-Meteo supplementation.
- Schedule the MetYr refresh every 5 minutes and the Open-Meteo supplement every 10 minutes.
- Keep expired forecast cleanup owned by the MetYr workflow.
- Preserve the unique forecast-cache key on `(LocationId, Time)` while enforcing provider precedence in persistence rather than relying on worker-side merge behavior.
- Ensure a conflicting Open-Meteo upsert cannot overwrite an existing Yr-backed row.
- Ensure a later Yr upsert can overwrite or supersede an existing Open-Meteo-backed row for the same `(LocationId, Time)` key.
- Give the Open-Meteo workflow its own location-selection strategy: locations with no Open-Meteo supplement rows first, then locations whose Open-Meteo-backed forecast rows were updated longest ago.
- Keep landing forecast enrichment and all authoritative near-term forecast ownership inside the MetYr workflow.

**Non-Goals:**
- Introduce a second forecast table, provider-specific cache table, or schema split by provider.
- Change the current Open-Meteo request shape, coordinate truncation rule, or takeoff-only supplementation scope.
- Make Open-Meteo responsible for landing forecast fields.
- Change startup health-check posture or add new credentials.
- Broaden this change into provider-mapping refinements unrelated to scheduling, selection, or precedence.

## Architecture

```text
                    recurring worker schedules
                              |
             +----------------+----------------+
             |                                 |
             v                                 v
      MetYr forecast refresh           Open-Meteo supplement refresh
      - every 5 minutes                - every 10 minutes
      - owns expired cleanup           - no cleanup
      - authoritative writer           - supplemental writer
      - takeoff + optional landing     - takeoff only
             |                                 |
             v                                 v
      map provider data                 map provider data
             |                                 |
             +----------------+----------------+
                              |
                              v
                 forecast-cache repository upsert
                 - unique by (LocationId, Time)
                 - Yr row may replace Open-Meteo row
                 - Open-Meteo row may not replace Yr row
```

The core change is not only that there are two worker services. It is that both services intentionally converge on one persistence path, and that path must enforce the precedence invariant regardless of which service runs first.

## Service Split Design

### MetYr refresh workflow

The MetYr workflow becomes the authoritative forecast refresh path. Its responsibilities are:

1. delete expired forecast rows before selecting locations
2. select candidate active main paragliding locations using the existing authoritative freshness strategy
3. fetch and map MetYr takeoff forecasts sequentially per location
4. enrich landing forecast fields from Yr where landing coordinates exist
5. upsert Yr-backed forecast rows

MetYr remains the source of truth for overlapping timestamps and the owner of cleanup because forecast retention is a cache-lifecycle responsibility, not an Open-Meteo supplementation responsibility.

### Open-Meteo supplement workflow

The Open-Meteo workflow becomes an independent lower-frequency supplement path. Its responsibilities are:

1. select candidate active main paragliding locations using Open-Meteo-specific freshness criteria
2. fetch one batched Open-Meteo request for the selected takeoff coordinates
3. map the returned takeoff forecast rows as Open-Meteo-backed forecast cache entries
4. upsert only through the shared forecast-cache repository contract

The Open-Meteo workflow does not own cleanup, does not populate landing fields, and does not need worker-side overlap filtering to preserve Yr precedence. It may attempt writes for timestamps already present from Yr, but those writes must be blocked by the repository contract.

## Persistence Design

### Required invariant

The forecast-cache repository must enforce this row-level rule for conflicts on `(LocationId, Time)`:

- an incoming Yr row is allowed to update an existing row regardless of whether the existing row is Yr-backed or Open-Meteo-backed
- an incoming Open-Meteo row is allowed to update an existing row only when the existing row is not Yr-backed

Equivalent gating rule:

- skip the update only when `existing.IsYrData == true` and `incoming.IsYrData == false`

This is the invariant that makes the split design safe.

### Why row-level gating is the correct shape

The repository does not need field-by-field provider arbitration. The business rule is simpler than that:

- if the conflicting row already comes from Yr, Open-Meteo should do nothing
- if the conflicting row comes from Open-Meteo and Yr later writes the same timestamp, Yr should replace it

That means the repository needs conditional conflict updates, not merged provider fields. A whole-row allow-or-skip rule is enough.

### Viability of the upsert seam

FlexLabs upsert support appears sufficient for this design because it exposes:

- a two-parameter matched update expression using existing and incoming rows
- an update condition that can gate whether the conflict update occurs at all

The design should therefore use the repository as the enforcement point for provider precedence, with the understanding that implementation must prove PostgreSQL translation and behavior using focused repository integration tests.

## Location Selection Design

The current shared selection strategy is based on generic forecast freshness. That is no longer enough once the workflows have different goals.

### MetYr selection

The MetYr workflow should keep the current behavior that prioritizes:

1. locations without forecast coverage
2. then locations with the oldest forecast data

This remains the correct selection model for the authoritative five-minute refresh.

### Open-Meteo selection

The Open-Meteo workflow should prioritize longer-horizon supplement gaps using one universal tail-ordering query:

1. locations with no Open-Meteo-backed forecast rows first because they have no forecast tail
2. then locations whose latest Open-Meteo-backed forecast timestamp is earliest

The controlling signal is the latest Open-Meteo-backed forecast `Time`, not `UpdatedAt`. Recent Yr writes should not make a location look fresh for Open-Meteo supplementation, and recent Open-Meteo write time is only a proxy for the horizon that actually matters. The query or repository abstraction therefore needs to distinguish Open-Meteo-backed rows from Yr-backed rows and order by the shortest remaining Open-Meteo forecast tail.

## Scheduling Design

Worker orchestration should expose two distinct recurring forecast jobs:

- MetYr refresh: every 5 minutes
- Open-Meteo supplement: every 10 minutes

This split is intentional:

- MetYr cadence protects near-term authoritative freshness
- Open-Meteo cadence reduces unnecessary batch churn and better aligns with quota pressure

The design does not depend on one schedule always running before the other. The repository precedence rule is what guarantees correctness when schedules overlap or drift.

## Failure Behavior

The workflows should fail independently.

- If the Open-Meteo batch fails, the MetYr workflow continues to refresh authoritative rows on its normal cadence.
- If the MetYr workflow fails for a cycle, Open-Meteo may still refresh its supplemental horizon, but it still cannot overwrite existing Yr-backed rows.
- Cleanup remains tied to the MetYr workflow, so Open-Meteo failure does not affect expired-row deletion ownership.

This preserves graceful degradation while preventing a lower-quality source from taking over overlapping timestamps during partial outages.

## Risks / Trade-offs

- The split increases orchestration complexity because there are now two forecast workflows, two selection strategies, and one shared repository invariant that must stay correct.
- The design depends on provider provenance remaining explicit in persisted rows through `IsYrData`. If that flag becomes unreliable, precedence enforcement becomes unreliable.
- The repository invariant is conceptually simple, but it still needs PostgreSQL-backed tests to prove that the chosen upsert library translates the conditional update correctly.
- Open-Meteo-specific selection will likely require a new repository query or database view rather than reusing the current generic oldest-forecast selector.

## Validation Strategy

Implementation should not be considered complete until it proves the precedence invariant at the repository layer.

The critical executable checks are:

- repository integration test: existing Yr row plus incoming Open-Meteo row for the same `(LocationId, Time)` leaves the Yr-backed row unchanged
- repository integration test: existing Open-Meteo row plus incoming Yr row for the same `(LocationId, Time)` allows the Yr-backed row to replace it
- service-level tests covering independent MetYr and Open-Meteo scheduling behavior and ownership boundaries
- selection tests for the Open-Meteo-specific priority rules
- executable validation with `openspec validate --specs`, `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj`, and `dotnet build WindLordApi.sln` after implementation