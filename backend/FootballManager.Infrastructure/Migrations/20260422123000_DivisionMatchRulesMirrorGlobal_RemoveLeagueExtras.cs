using System;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations;

/// <inheritdoc />
[DbContext(typeof(FootballManagerDbContext))]
[Migration("20260422123000_DivisionMatchRulesMirrorGlobal_RemoveLeagueExtras")]
public partial class DivisionMatchRulesMirrorGlobal_RemoveLeagueExtras : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "league_match_rules");

        migrationBuilder.DropColumn(
            name: "match_duration_minutes",
            table: "division_match_rules");

        migrationBuilder.DropColumn(
            name: "allowed_field_ids_json",
            table: "division_match_rules");

        migrationBuilder.AddColumn<int>(
            name: "half_minutes",
            table: "division_match_rules",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "halftime_break_minutes",
            table: "division_match_rules",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "warmup_buffer_minutes",
            table: "division_match_rules",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "slot_granularity_minutes",
            table: "division_match_rules",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "first_match_tolerance_minutes",
            table: "division_match_rules",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "first_match_tolerance_minutes", table: "division_match_rules");
        migrationBuilder.DropColumn(name: "slot_granularity_minutes", table: "division_match_rules");
        migrationBuilder.DropColumn(name: "warmup_buffer_minutes", table: "division_match_rules");
        migrationBuilder.DropColumn(name: "halftime_break_minutes", table: "division_match_rules");
        migrationBuilder.DropColumn(name: "half_minutes", table: "division_match_rules");

        migrationBuilder.AddColumn<int>(
            name: "match_duration_minutes",
            table: "division_match_rules",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "allowed_field_ids_json",
            table: "division_match_rules",
            type: "text",
            nullable: true);

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
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
}
