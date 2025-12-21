using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WindLordApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class ViewLocationsWithNoAndOldestForecast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create view for locations with oldest forecast
            migrationBuilder.Sql(@"
        CREATE OR REPLACE VIEW public.locations_with_oldest_forecast AS 
        SELECT fc.location_id,
            min(fc.updated_at) AS oldest_updated_at
        FROM forecast_cache fc
            JOIN all_paragliding_locations apl ON fc.location_id = apl.id
        WHERE apl.is_main = true
        GROUP BY fc.location_id
        ORDER BY (min(fc.updated_at));
    ");

            // Create view for locations without forecast
            migrationBuilder.Sql(@"
        CREATE OR REPLACE VIEW public.locations_without_forecast AS 
        SELECT apl.id AS location_id,
            apl.name,
            apl.latitude,
            apl.longitude
        FROM all_paragliding_locations apl
            LEFT JOIN forecast_cache fc ON apl.id = fc.location_id
        WHERE apl.is_active = true AND apl.is_main = true AND fc.location_id IS NULL
        ORDER BY apl.name;
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop views
            migrationBuilder.Sql("DROP VIEW IF EXISTS public.locations_without_forecast;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS public.locations_with_oldest_forecast;");
        }
    }
}
