# Domain Context

- WindLordApi aggregates weather and geocoding data for paragliding locations.
- Use `ParaglidingLocation`, `WeatherStation`, `ForecastCache`, `StationData`, `LatestStationData`, and `Provider` consistently.
- Forecast cleanup happens before refresh, metadata is written before dependent observations, and latest-station rows are derived from observation history.
- `openspec/specs/` is the behavioral source of truth.