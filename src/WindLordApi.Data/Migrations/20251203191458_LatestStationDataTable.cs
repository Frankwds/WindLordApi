using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WindLordApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class LatestStationDataTable : Migration
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

            migrationBuilder.CreateIndex(
                name: "latest_station_data_station_id_key",
                table: "latest_station_data",
                column: "station_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "latest_station_data");
        }
    }
}
