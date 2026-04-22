using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HierarchicalMatchSchedulingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "division_match_rules",
                columns: table => new
                {
                    division_season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    break_between_matches_minutes = table.Column<int>(type: "integer", nullable: true),
                    allowed_time_ranges_json = table.Column<string>(type: "text", nullable: true),
                    allowed_field_ids_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_division_match_rules", x => x.division_season_id);
                    table.ForeignKey(
                        name: "FK_division_match_rules_division_seasons_division_season_id",
                        column: x => x.division_season_id,
                        principalTable: "division_seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "division_season_fields",
                columns: table => new
                {
                    division_season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_division_season_fields", x => new { x.division_season_id, x.field_id });
                    table.ForeignKey(
                        name: "FK_division_season_fields_division_seasons_division_season_id",
                        column: x => x.division_season_id,
                        principalTable: "division_seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_division_season_fields_fields_field_id",
                        column: x => x.field_id,
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "league_match_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_match_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    default_break_between_matches_minutes = table.Column<int>(type: "integer", nullable: true),
                    default_allowed_time_ranges_json = table.Column<string>(type: "text", nullable: true),
                    default_allowed_field_ids_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_match_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_league_match_rules_leagues_league_id",
                        column: x => x.league_id,
                        principalTable: "leagues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_league_match_rules_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_division_season_fields_field_id",
                table: "division_season_fields",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "IX_league_match_rules_league_id",
                table: "league_match_rules",
                column: "league_id",
                unique: true,
                filter: "season_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_league_match_rules_league_id_season_id",
                table: "league_match_rules",
                columns: new[] { "league_id", "season_id" },
                unique: true,
                filter: "season_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_league_match_rules_season_id",
                table: "league_match_rules",
                column: "season_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "division_match_rules");

            migrationBuilder.DropTable(
                name: "division_season_fields");

            migrationBuilder.DropTable(
                name: "league_match_rules");
        }
    }
}
