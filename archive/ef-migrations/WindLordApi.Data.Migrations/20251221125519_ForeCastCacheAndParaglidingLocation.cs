using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WindLordApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class ForeCastCacheAndParaglidingLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "all_paragliding_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    longitude = table.Column<float>(type: "real", nullable: false),
                    latitude = table.Column<float>(type: "real", nullable: false),
                    altitude = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Norway"),
                    flightlog_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    n = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ne = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    e = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    se = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    s = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sw = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    w = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nw = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_main = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    landing_latitude = table.Column<float>(type: "real", nullable: true),
                    landing_longitude = table.Column<float>(type: "real", nullable: true),
                    landing_altitude = table.Column<int>(type: "integer", nullable: true),
                    timezone = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_all_paragliding_locations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "forecast_cache",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    temperature = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    wind_speed = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    wind_direction = table.Column<int>(type: "integer", nullable: true),
                    wind_gusts = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    precipitation = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    precipitation_probability = table.Column<float>(type: "real", nullable: true),
                    pressure_msl = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    weather_code = table.Column<string>(type: "text", nullable: true),
                    is_day = table.Column<short>(type: "smallint", nullable: true),
                    landing_wind = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    landing_gust = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    landing_wind_direction = table.Column<int>(type: "integer", nullable: true),
                    wind_speed_1000hpa = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    wind_direction_1000hpa = table.Column<int>(type: "integer", nullable: true),
                    wind_speed_925hpa = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    wind_direction_925hpa = table.Column<int>(type: "integer", nullable: true),
                    wind_speed_850hpa = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    wind_direction_850hpa = table.Column<int>(type: "integer", nullable: true),
                    wind_speed_700hpa = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    wind_direction_700hpa = table.Column<int>(type: "integer", nullable: true),
                    temperature_1000hpa = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    temperature_925hpa = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    temperature_850hpa = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    temperature_700hpa = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    cloud_cover = table.Column<int>(type: "integer", nullable: true),
                    cloud_cover_low = table.Column<int>(type: "integer", nullable: true),
                    cloud_cover_mid = table.Column<int>(type: "integer", nullable: true),
                    cloud_cover_high = table.Column<int>(type: "integer", nullable: true),
                    cape = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    convective_inhibition = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    lifted_index = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    boundary_layer_height = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    freezing_level_height = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    geopotential_height_1000hpa = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    geopotential_height_925hpa = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    geopotential_height_850hpa = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    geopotential_height_700hpa = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    precipitation_max = table.Column<double>(type: "double precision", nullable: true),
                    precipitation_min = table.Column<double>(type: "double precision", nullable: true),
                    is_yr_data = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forecast_cache", x => x.id);
                    table.UniqueConstraint("forecast_cache_location_id_time_key", x => new { x.location_id, x.time });
                    table.CheckConstraint("forecast_cache_is_day_check", "is_day = ANY (ARRAY[0, 1])");
                    table.ForeignKey(
                        name: "forecast_cache_location_id_fkey",
                        column: x => x.location_id,
                        principalTable: "all_paragliding_locations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "all_paragliding_locations_country_is_active_idx",
                table: "all_paragliding_locations",
                columns: new[] { "country", "is_active" },
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "all_paragliding_locations_flightlog_id_key",
                table: "all_paragliding_locations",
                column: "flightlog_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "all_paragliding_locations_is_active_latitude_longitude_idx",
                table: "all_paragliding_locations",
                columns: new[] { "is_active", "latitude", "longitude" },
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "all_paragliding_locations_is_main_idx",
                table: "all_paragliding_locations",
                column: "is_main");

            migrationBuilder.CreateIndex(
                name: "all_paragliding_locations_name_idx",
                table: "all_paragliding_locations",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "forecast_cache_location_id_idx",
                table: "forecast_cache",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "idx_forecast_cache_location_time",
                table: "forecast_cache",
                columns: new[] { "location_id", "time" });

            migrationBuilder.CreateIndex(
                name: "idx_forecast_cache_time",
                table: "forecast_cache",
                column: "time");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "forecast_cache");

            migrationBuilder.DropTable(
                name: "all_paragliding_locations");
        }
    }
}
