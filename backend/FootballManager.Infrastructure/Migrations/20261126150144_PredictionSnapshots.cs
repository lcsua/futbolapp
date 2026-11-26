using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PredictionSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prediction_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    season_slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    division_slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cutoff_round = table.Column<int>(type: "integer", nullable: false),
                    reconstructed = table.Column<bool>(type: "boolean", nullable: false),
                    title_probability = table.Column<double>(type: "double precision", nullable: false),
                    podium_probability = table.Column<double>(type: "double precision", nullable: false),
                    expected_position = table.Column<double>(type: "double precision", nullable: false),
                    expected_points = table.Column<double>(type: "double precision", nullable: false),
                    model_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prediction_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_prediction_snapshots_cutoff",
                table: "prediction_snapshots",
                columns: new[] { "league_slug", "season_slug", "division_slug", "team_id", "cutoff_round", "model_version", "reconstructed" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prediction_snapshots");
        }
    }
}
