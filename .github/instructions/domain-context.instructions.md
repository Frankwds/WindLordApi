---
description: Project domain context, vocabulary, and invariants for WindLordApi
applyTo: "**/*"
---

# Domain Context

## Project Overview
WindLordApi is a .NET 9 background worker that aggregates weather and location data for paragliding locations. It runs scheduled sync jobs against external providers, normalizes that data, and persists forecasts and station observations to PostgreSQL.

## Domain Vocabulary

Use these terms consistently throughout the codebase:

| Term | Definition | Do NOT use |
|------|------------|------------|
| ParaglidingLocation | A geographic site the worker tracks for forecast and enrichment workflows | Site record, waypoint |
| WeatherStation | A provider-backed station metadata record used for observations | Sensor row, source node |
| ForecastCache | Persisted forecast rows for a paragliding location | Weather snapshot table |
| StationData | Historical normalized observation rows for a weather station | Live row, raw payload |
| LatestStationData | Derived current snapshot built from station observations | Canonical source table |
| Provider | An upstream external source such as MetYr, MetFrost, Holfuy, WindsMobi, or Google Geocoding | Vendor blob |

## Business Rules

- Forecast cleanup MUST happen before fresh forecast rows are written.
- Weather-station metadata MUST be available before observations for newly discovered stations are persisted.
- Latest station data is derived from normalized observation history, not treated as an independent source of truth.
- Active locations and station activity state drive recurring sync behavior.
- Batching is part of the system behavior because provider and database limits shape how sync work is performed.

## Specifications

Behavioral specifications live in `openspec/specs/`. Always read the relevant spec before changing behavior. If the spec is wrong or incomplete, update it through `/opsx:propose` rather than coding around it.