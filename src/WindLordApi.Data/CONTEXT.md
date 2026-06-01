# CONTEXT.md

## Scope
- Applies to EF Core model configuration, repositories, services, archived migration history, and any change that depends on PostgreSQL semantics rather than in-memory behavior. `confirmed`

## Language
- `StationId` is the durable provider key used for weather-station relationships and upsert matching. Entity `Id` is not the cross-layer provider key. `confirmed`
- `Latest station data` is its own read-optimized store, not "the latest row in station_data". `confirmed`
- Forecast refresh candidate selection is implemented as inline repository queries over `ParaglidingLocation` and `ForecastCache`, not keyless EF view entities. `confirmed`

## Local Intent
- This layer keeps persistence rules, transactional batching, and PostgreSQL-specific behavior out of Worker and Integrations. `confirmed`
- If a change affects keys, check constraints, inline selection queries, or upsert match columns, assume repository behavior and integration tests are coupled to it. `confirmed`

## Structure
- `ApplicationDbContext.cs` is the source of truth for keys, indexes, check constraints, and default values. Repository queries own selection-specific projections and ordering. `confirmed`
- Repositories hold the actual upsert and update projections; services add input validation, batching, and explicit transaction boundaries. `confirmed`
- `Extensions/ConfigurationExtensions.cs` owns environment-based connection-string selection. `confirmed`
- Archived EF migrations live under `archive/ef-migrations/` and are retained as historical reference only, not as the active schema workflow. `confirmed`

## Local Rules
- `WeatherStation` is unique by `StationId`. Normal weather-station upserts update metadata but intentionally do not overwrite `Country` or `IsMain`; those are reserved for `UpdateCountriesAsync`. `confirmed`
- Normal weather-station upsert also preserves existing `IsActive`, except Holfuy inputs force `IsActive = true`. Provider active-state workflows own the rest of the active-flag lifecycle. `confirmed`
- `StationData` upsert matching is `(StationId, UpdatedAt)`. `ForecastCache` upsert matching is `(LocationId, Time)`. Breaking those keys breaks idempotency. `confirmed`
- `ForecastCache` upsert also enforces provider precedence: an incoming Open-Meteo row does not overwrite an existing Yr-backed row for the same `(LocationId, Time)`, but a later Yr-backed row can replace an Open-Meteo-backed row. `confirmed`
- `GetStationsWithMissingCountryAsync` treats both `null` and `"UKJENT"` as missing. `confirmed`
- `GetByIdsAsync` for paragliding locations returns only rows where `IsActive && IsMain`, so both MetYr refresh and Open-Meteo supplementation ignore inactive or secondary locations even if their IDs were selected earlier. `confirmed`
- `WeatherStationService` and `ForecastCacheService` batch at 1000 records and wrap each batch in an explicit transaction for Supabase pooler compatibility. `confirmed`
- DateTime storage is not uniform across tables. `WeatherStation.UpdatedAt` and the forecast/station snapshot tables use PostgreSQL `timestamp with time zone`, while other fields may still use `timestamp without time zone`. Treat DateTime-kind edits as persistence changes, not cleanup. `confirmed`

## Validation
- Use `dotnet test src/WindLordApi.Tests/WindLordApi.Tests.csproj` for repository or service changes, but prefer the integration tests under `src/WindLordApi.Tests/Integration` when touching upserts, deletes, transactions, views, or filtered indexes. `confirmed`
- Use `dotnet build WindLordApi.sln` after model or repository changes. `confirmed`
- Upstream schema changes should be validated through the startup schema-contract health checks because the normal test harness uses `EnsureCreatedAsync` from the current EF model. `confirmed`

## Watchouts
- Forecast refresh selection now lives inline in `ParaglidingLocationRepository` for both MetYr and Open-Meteo. Changes to selection ordering, missing-coverage priority, or freshness semantics are repository behavior changes even when no EF model changes are involved. `confirmed`
- FlexLabs upsert match columns must stay aligned across model configuration, repository `.On(...)` clauses, and database constraints or indexes. `confirmed`
- A repository update projection is usually intentional. If a field is absent in `WhenMatched`, check whether another workflow owns it before adding it. `confirmed`