using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WindLordApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "latest_station_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    station_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    wind_speed = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    wind_gust = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    temperature = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    wind_min_speed = table.Column<decimal>(type: "numeric(5,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_latest_station_data", x => x.id);
                    table.CheckConstraint("latest_station_data_direction_check", "direction >= 0 AND direction <= 360");
                });

            migrationBuilder.CreateTable(
                name: "weather_stations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(10,8)", nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(11,8)", nullable: false),
                    altitude = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    provider = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    station_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_main = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather_stations", x => x.id);
                    table.UniqueConstraint("AK_weather_stations_station_id", x => x.station_id);
                    table.CheckConstraint("check_provider_not_empty", "provider IS NOT NULL AND provider <> ''");
                });

            migrationBuilder.CreateTable(
                name: "station_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    wind_speed = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    wind_gust = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    wind_min_speed = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    temperature = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    is_compressed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    station_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_station_data", x => x.id);
                    table.UniqueConstraint("AK_station_data_station_id_updated_at", x => new { x.station_id, x.updated_at });
                    table.CheckConstraint("station_data_direction_check", "direction >= 0 AND direction <= 360");
                    table.ForeignKey(
                        name: "fk_station_data_station_id",
                        column: x => x.station_id,
                        principalTable: "weather_stations",
                        principalColumn: "station_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "latest_station_data_station_id_key",
                table: "latest_station_data",
                column: "station_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "unique_station_timestamp",
                table: "station_data",
                columns: new[] { "station_id", "updated_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "weather_stations_station_id_unique",
                table: "weather_stations",
                column: "station_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "latest_station_data");

            migrationBuilder.DropTable(
                name: "station_data");

            migrationBuilder.DropTable(
                name: "weather_stations");
        }
    }
}
