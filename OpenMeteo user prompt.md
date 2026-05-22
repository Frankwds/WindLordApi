Open-Meteo integration.

WindLord is a map frontend that consumes data populated in the database by WindLordApi. We are already caching forecast data from yr. All the data from yr is seen as very high quality and is always the preferred forecast to keep. But we only have roughly 48 hours of hourly weather data from yr. That is why we are connecting to Open-Meteo to supplement and extend the range of forecast we can show our users.

There will always be at least 48 hours of yr data per location. Maybe up to 56, but it varies.
We are going to get forecast from two days in the future to four days in the future when we fetch from Open-Meteo.
We are going to use the service ForecastUpdateService to coordinate the fetch and merger of Yr and Open-Meteo integration.
Open-Meteo can return multiple locations in one request. We should batch all selected takeoff locations into the same Open-Meteo request whenever practical, then merge the returned per-location payloads with the per-location Yr results before upsert.
The ForecastCache table has a column "IsYrData" that should be set correctly.

Transferred decisions that still apply here:
- Yr remains the preferred forecast source wherever it has coverage.
- Open-Meteo should only supplement timestamps that are strictly later than the latest Yr timestamp returned for that location in the current refresh run.
- The supplement window is rolling hours from now, not calendar-day slices.
- If Open-Meteo fails but Yr succeeds, we still write the Yr-derived rows.
- If Yr fails for a location, we skip persistence for that location even if Open-Meteo succeeded.
- Open-Meteo rows are takeoff-only. Landing forecast data should still come only from Yr.
- Unknown or unsupported weather-code mappings should stay null instead of being coerced.
- We want UTC timestamps.
- Open-Meteo should have the same level of startup health check as the other external integrations.

1. API Endpoint and Parameters
Base URL: https://api.open-meteo.com/v1/forecast

latitude (Required): One or more comma-separated latitudes.

longitude (Required): One or more comma-separated longitudes in the same order as latitude.

hourly (Required for our use case): Comma-separated hourly variables. Based on what `ForecastUpdateService` currently persists from Yr takeoff forecasts, the exact Open-Meteo fields we need are `temperature_2m`, `wind_speed_10m`, `wind_direction_10m`, `wind_gusts_10m`, `precipitation`, `precipitation_probability`, `pressure_msl`, `weather_code`, and `is_day`.

Fields we do not need for the first Open-Meteo integration, even though the schema has columns for some of them: `cloud_cover`, `cloud_cover_low`, `cloud_cover_mid`, `cloud_cover_high`, pressure-level wind fields, pressure-level temperature fields, geopotential heights, CAPE, convective inhibition, lifted index, boundary layer height, and freezing level height. The current Yr-to-cache conversion does not populate those fields today.

Important mismatch with current Yr persistence: the worker currently stores `precipitation_max` and `precipitation_min` from Yr, but Open-Meteo's hourly forecast API does not expose hourly min/max precipitation fields. Unless we define a new rule, those should remain null on Open-Meteo rows.

start_hour / end_hour: ISO 8601 hour boundaries for rolling-hour requests. These are a better fit than `start_date` / `end_date` because we want a rolling UTC window rather than local calendar days.

wind_speed_unit: Set to `ms` so the returned wind speed and gust values match the units already expected in our forecast cache.

timezone: Use `GMT` or equivalent UTC behavior so timestamps are easy to compare with the latest Yr timestamp for each location.

The endpoint can also take `start_date` and `end_date`, but that is less precise than `start_hour` / `end_hour` for this use case.

2. Example API Call
If you want to fetch the supplement window for multiple locations in one request, an HTTP GET can look like this:

HTTP
https://api.open-meteo.com/v1/forecast?latitude=52.52,53.52&longitude=13.41,14.41&hourly=temperature_2m,wind_speed_10m,wind_direction_10m,wind_gusts_10m,precipitation,precipitation_probability,pressure_msl,weather_code,is_day&start_hour=2026-05-24T00:00&end_hour=2026-05-25T23:00&wind_speed_unit=ms&timezone=GMT

The endpoint returns valid JSON. For multiple coordinates, the response becomes a list of structures. Each structure contains hourly arrays, including `hourly.time` plus one array per requested hourly field. For this integration that means arrays for `temperature_2m`, `wind_speed_10m`, `wind_direction_10m`, `wind_gusts_10m`, `precipitation`, `precipitation_probability`, `pressure_msl`, `weather_code`, and `is_day`.

Representative payload shape when we request the exact field list above:
```json
[
	{
		"latitude": 52.52,
		"longitude": 13.419998,
		"utc_offset_seconds": 0,
		"timezone": "GMT",
		"timezone_abbreviation": "GMT",
		"elevation": 38.0,
		"hourly_units": {
			"time": "iso8601",
			"temperature_2m": "°C",
			"wind_speed_10m": "m/s",
			"wind_direction_10m": "°",
			"wind_gusts_10m": "m/s",
			"precipitation": "mm",
			"precipitation_probability": "%",
			"pressure_msl": "hPa",
			"weather_code": "wmo code",
			"is_day": "dimensionless"
		},
		"hourly": {
			"time": ["2026-05-24T00:00", "2026-05-24T01:00"],
			"temperature_2m": [20.3, 19.6],
			"wind_speed_10m": [1.1, 1.2],
			"wind_direction_10m": [304, 301],
			"wind_gusts_10m": [2.4, 2.8],
			"precipitation": [0.0, 0.2],
			"precipitation_probability": [5, 10],
			"pressure_msl": [1017.2, 1017.0],
			"weather_code": [1, 3],
			"is_day": [0, 0]
		}
	}
]
```

Open-Meteo does not return icon strings. It returns numeric WMO `weather_code` values, and it can also return `is_day` as a separate hourly field.

Since we already are consuming forecast from yr, we will map Open-Meteo `weather_code` plus `is_day` to the format from yr so WindLord can consume it effortlessly.

Open-Meteo uses WMO weather interpretation codes, for example:
- `0`: Clear sky
- `1, 2, 3`: Mainly clear, partly cloudy, overcast
- `45, 48`: Fog
- `51, 53, 55`: Drizzle
- `56, 57`: Freezing drizzle
- `61, 63, 65`: Rain
- `66, 67`: Freezing rain
- `71, 73, 75, 77`: Snow and snow grains
- `80, 81, 82`: Rain showers
- `85, 86`: Snow showers
- `95`: Thunderstorm
- `96, 99`: Thunderstorm with hail

Locked WMO-to-Yr symbol mapping table for this integration:

| Open-Meteo WMO code(s) | Use `is_day`? | Target weather code | Reason |
| --- | --- | --- | --- |
| `0` | Yes | `clearsky_day` / `clearsky_night` | Direct clear-sky day/night mapping |
| `1`, `2` | Yes | `partlycloudy_day` / `partlycloudy_night` | Closest supported partly-cloudy fallback |
| `3` | No | `cloudy` | Overcast / fully cloudy |
| `45`, `48` | No | `fog` | Direct fog mapping |
| `51`, `53`, `55` | No | `rain` | Drizzle falls back to rain |
| `56`, `57` | No | `sleet` | Freezing drizzle is closest to sleet in the current vocabulary |
| `61`, `63`, `65` | No | `rain` | Rain |
| `66`, `67` | No | `sleet` | Freezing rain is closest to sleet |
| `71`, `73`, `75`, `77` | No | `snow` | Snow and snow grains |
| `80`, `81`, `82` | No | `rain` | Rain showers fall back to rain |
| `85`, `86` | No | `snow` | Snow showers fall back to snow |
| `95` | No | `rainandthunder` | Thunder is the most important signal |
| `96`, `99` | No | `rainandthunder` | Thunderstorm with hail still maps to the thunder code because hail has no dedicated target |
| Any other code | No | `null` | Unknown / unsupported values should stay unset |

Additional mapping rules:
- Only `0`, `1`, and `2` use `is_day` to choose day/night variants.
- All other mapped weather codes are time-agnostic in the current vocabulary.
- We are intentionally not introducing new Yr-style symbols such as `fair_day`, `lightrain`, `heavyrain`, `sleetandthunder`, or `snowandthunder` for this integration.

Notes from this payload that matter for implementation:
- The response is a list of per-location forecast structures, not one combined hourly table.
- Returned hourly arrays are aligned by index within each location block.
- A `location_id` may appear in batched results, but we should not treat it as the primary contract for correlation. Request order plus returned coordinates should still be enough to map results back to selected locations.
- The example payload shows `km/h` units because `wind_speed_unit=ms` was not requested there. In our implementation we want `wind_speed_unit=ms`.
- The example payload does not include all of our intended hourly fields because it is only an example. The real integration request should include the exact hourly field list above so the payload also contains `precipitation`, `precipitation_probability`, `pressure_msl`, and `is_day` arrays.

That means we need a WMO-to-Yr mapping layer instead of the previous icon-to-Yr mapping layer. The table above is the locked mapping for the first Open-Meteo integration.

Important Open-Meteo-specific design notes discovered from the docs:
- Multiple locations can be fetched in one request by comma-separating `latitude` and `longitude`.
- For multi-location JSON responses, the implementation needs a stable way to correlate each returned structure back to the selected paragliding location. Request order is the safest primary key, with returned coordinates as a sanity check.
- The generic forecast endpoint automatically stitches the best available weather models for each location and can continue beyond the short MET Nordic horizon, which is useful for northern Norway.
- `wind_speed_unit=ms` should be used to avoid unit conversion mismatches with the current forecast cache expectations.
- `start_hour` / `end_hour` is preferable to `start_date` / `end_date` because our supplement window is defined as rolling hours, not local calendar dates.