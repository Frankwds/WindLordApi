<!-- graphify-metadata
community: mixed-supporting-nodes (location-enrichment)
god_nodes: ICountryLocatorService, ParaglidingLocation, GoogleGeocodingClient
bridge_nodes: ParaglidingLocationService
related_tables: all_paragliding_locations
-->

# Location Enrichment

## Purpose
Enrich paragliding locations with country metadata derived from their stored geographic coordinates.

## Requirements

### Requirement: Reverse Geocode Locations That Need Country Data
Locations that are missing country information SHALL be reverse geocoded from their stored coordinates.

#### Scenario: A location is missing its country value
Given a paragliding location with latitude and longitude but no country
When the location-enrichment workflow runs
Then the worker SHOULD request country information from the geocoding integration for that location

### Requirement: Update Existing Location Records
Country enrichment SHALL update the existing paragliding-location record instead of creating a duplicate location entry.

#### Scenario: A geocoding lookup succeeds
Given an existing paragliding location selected for enrichment
When the geocoding client returns a country result
Then the workflow SHALL persist the country value onto that same paragliding-location record

### Requirement: Use Stored Coordinates As The Lookup Input
The enrichment workflow MUST use the location coordinates already stored for the paragliding location as the geocoding input.

#### Scenario: The workflow prepares a geocoding request
Given a paragliding location selected for country lookup
When the geocoding client request is constructed
Then the request MUST use that location's persisted latitude and longitude values