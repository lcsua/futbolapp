using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixtureEngineFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "league_id",
                table: "fixtures",
                type: "uuid",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "season_id",
                table: "fixtures",
                type: "uuid",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "home_team_division_season_id",
                table: "fixtures",
                type: "uuid",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "away_team_division_season_id",
                table: "fixtures",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE fixtures f
                SET
                    league_id = (SELECT s.league_id FROM division_seasons ds JOIN seasons s ON s.id = ds.season_id WHERE ds.id = f.division_season_id LIMIT 1),
                    season_id = (SELECT ds.season_id FROM division_seasons ds WHERE ds.id = f.division_season_id LIMIT 1),
                    home_team_division_season_id = (SELECT tds.id FROM team_division_seasons tds WHERE tds.team_id = f.home_team_id AND tds.division_season_id = f.division_season_id LIMIT 1),
                    away_team_division_season_id = (SELECT tds.id FROM team_division_seasons tds WHERE tds.team_id = f.away_team_id AND tds.division_season_id = f.division_season_id LIMIT 1)
                WHERE f.league_id IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE fixtures f
                SET field_id = (SELECT fl.id FROM fields fl WHERE fl.league_id = f.league_id LIMIT 1)
                WHERE f.field_id IS NULL AND f.league_id IS NOT NULL AND EXISTS (SELECT 1 FROM fields fl WHERE fl.league_id = f.league_id LIMIT 1);
            ");
            migrationBuilder.Sql("UPDATE fixtures SET round_number = 1 WHERE round_number IS NULL;");
            migrationBuilder.Sql("DELETE FROM fixtures WHERE league_id IS NULL OR season_id IS NULL OR home_team_division_season_id IS NULL OR away_team_division_season_id IS NULL OR field_id IS NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_fixtures_fields_field_id",
                table: "fixtures");
            migrationBuilder.DropForeignKey(
                name: "FK_fixtures_teams_away_team_id",
                table: "fixtures");
            migrationBuilder.DropForeignKey(
                name: "FK_fixtures_teams_home_team_id",
                table: "fixtures");
            migrationBuilder.DropIndex(
                name: "IX_fixtures_away_team_id",
                table: "fixtures");
            migrationBuilder.DropIndex(
                name: "IX_fixtures_division_season_id",
                table: "fixtures");
            migrationBuilder.DropIndex(
                name: "IX_fixtures_field_id",
                table: "fixtures");
            migrationBuilder.DropIndex(
                name: "IX_fixtures_home_team_id",
                table: "fixtures");

            migrationBuilder.DropColumn(
                name: "home_team_id",
                table: "fixtures");
            migrationBuilder.DropColumn(
                name: "away_team_id",
                table: "fixtures");

            migrationBuilder.RenameColumn(
                name: "match_time",
                table: "fixtures",
                newName: "start_time");

            migrationBuilder.AlterColumn<Guid>(
                name: "league_id",
                table: "fixtures",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(
                name: "season_id",
                table: "fixtures",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(
                name: "home_team_division_season_id",
                table: "fixtures",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(
                name: "away_team_division_season_id",
                table: "fixtures",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
            migrationBuilder.AlterColumn<int>(
                name: "round_number",
                table: "fixtures",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(
                name: "field_id",
                table: "fixtures",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "fields",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "fields",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "fields",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "competition_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    matches_per_week = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_home_away = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competition_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_competition_rules_leagues_league_id",
                        column: x => x.league_id,
                        principalTable: "leagues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competition_rules_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "field_availability",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_availability", x => x.id);
                    table.ForeignKey(
                        name: "FK_field_availability_fields_field_id",
                        column: x => x.field_id,
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "field_blackouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_blackouts", x => x.id);
                    table.ForeignKey(
                        name: "FK_field_blackouts_fields_field_id",
                        column: x => x.field_id,
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    half_minutes = table.Column<int>(type: "integer", nullable: false),
                    break_minutes = table.Column<int>(type: "integer", nullable: false),
                    warmup_buffer_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    slot_granularity_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_match_rules_leagues_league_id",
                        column: x => x.league_id,
                        principalTable: "leagues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_match_rules_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competition_match_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competition_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competition_match_days", x => x.id);
                    table.ForeignKey(
                        name: "FK_competition_match_days_competition_rules_competition_rule_id",
                        column: x => x.competition_rule_id,
                        principalTable: "competition_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fixtures_away_team_division_season_id",
                table: "fixtures",
                column: "away_team_division_season_id");

            migrationBuilder.CreateIndex(
                name: "IX_fixtures_division_season_id_round_number",
                table: "fixtures",
                columns: new[] { "division_season_id", "round_number" });

            migrationBuilder.CreateIndex(
                name: "IX_fixtures_field_id_match_date",
                table: "fixtures",
                columns: new[] { "field_id", "match_date" });

            migrationBuilder.CreateIndex(
                name: "IX_fixtures_home_team_division_season_id",
                table: "fixtures",
                column: "home_team_division_season_id");

            migrationBuilder.CreateIndex(
                name: "IX_fixtures_league_id_season_id",
                table: "fixtures",
                columns: new[] { "league_id", "season_id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_fixtures_home_away_different",
                table: "fixtures",
                sql: "home_team_division_season_id != away_team_division_season_id");

            migrationBuilder.CreateIndex(
                name: "IX_competition_match_days_competition_rule_id",
                table: "competition_match_days",
                column: "competition_rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_competition_rules_league_id",
                table: "competition_rules",
                column: "league_id");

            migrationBuilder.CreateIndex(
                name: "IX_competition_rules_season_id",
                table: "competition_rules",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_field_availability_field_id",
                table: "field_availability",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "IX_field_blackouts_field_id",
                table: "field_blackouts",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "IX_match_rules_league_id",
                table: "match_rules",
                column: "league_id");

            migrationBuilder.CreateIndex(
                name: "IX_match_rules_season_id",
                table: "match_rules",
                column: "season_id");

            migrationBuilder.AddForeignKey(
                name: "FK_fixtures_fields_field_id",
                table: "fixtures",
                column: "field_id",
                principalTable: "fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fixtures_leagues_league_id",
                table: "fixtures",
                column: "league_id",
                principalTable: "leagues",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fixtures_seasons_season_id",
                table: "fixtures",
                column: "season_id",
                principalTable: "seasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fixtures_team_division_seasons_away_team_division_season_id",
                table: "fixtures",
                column: "away_team_division_season_id",
                principalTable: "team_division_seasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fixtures_team_division_seasons_home_team_division_season_id",
                table: "fixtures",
                column: "home_team_division_season_id",
                principalTable: "team_division_seasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fixtures_fields_field_id",
                table: "fixtures");

            migrationBuilder.DropForeignKey(
                name: "FK_fixtures_leagues_league_id",
                table: "fixtures");

            migrationBuilder.DropForeignKey(
                name: "FK_fixtures_seasons_season_id",
                table: "fixtures");

            migrationBuilder.DropForeignKey(
                name: "FK_fixtures_team_division_seasons_away_team_division_season_id",
                table: "fixtures");

            migrationBuilder.DropForeignKey(
                name: "FK_fixtures_team_division_seasons_home_team_division_season_id",
                table: "fixtures");

            migrationBuilder.DropTable(
                name: "competition_match_days");

            migrationBuilder.DropTable(
                name: "field_availability");

            migrationBuilder.DropTable(
                name: "field_blackouts");

            migrationBuilder.DropTable(
                name: "match_rules");

            migrationBuilder.DropTable(
                name: "competition_rules");

            migrationBuilder.DropIndex(
                name: "IX_fixtures_away_team_division_season_id",
                table: "fixtures");

            migrationBuilder.DropIndex(
                name: "IX_fixtures_division_season_id_round_number",
                table: "fixtures");

            migrationBuilder.DropIndex(
                name: "IX_fixtures_field_id_match_date",
                table: "fixtures");

            migrationBuilder.DropIndex(
                name: "IX_fixtures_home_team_division_season_id",
                table: "fixtures");

            migrationBuilder.DropIndex(
                name: "IX_fixtures_league_id_season_id",
                table: "fixtures");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fixtures_home_away_different",
                table: "fixtures");

            migrationBuilder.DropColumn(
                name: "away_team_division_season_id",
                table: "fixtures");
            migrationBuilder.DropColumn(
                name: "home_team_division_season_id",
                table: "fixtures");
            migrationBuilder.DropColumn(
                name: "league_id",
                table: "fixtures");
            migrationBuilder.DropColumn(
                name: "season_id",
                table: "fixtures");

            migrationBuilder.RenameColumn(
                name: "start_time",
                table: "fixtures",
                newName: "match_time");

            migrationBuilder.AddColumn<Guid>(
                name: "home_team_id",
                table: "fixtures",
                type: "uuid",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "away_team_id",
                table: "fixtures",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "round_number",
                table: "fixtures",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "field_id",
                table: "fixtures",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "fields",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "fields",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "fields",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_fixtures_away_team_id",
                table: "fixtures",
                column: "away_team_id");

            migrationBuilder.CreateIndex(
                name: "IX_fixtures_division_season_id",
                table: "fixtures",
                column: "division_season_id");

            migrationBuilder.CreateIndex(
                name: "IX_fixtures_field_id",
                table: "fixtures",
                column: "field_id");

            migrationBuilder.AddForeignKey(
                name: "FK_fixtures_fields_field_id",
                table: "fixtures",
                column: "field_id",
                principalTable: "fields",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_fixtures_teams_away_team_id",
                table: "fixtures",
                column: "away_team_id",
                principalTable: "teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fixtures_teams_home_team_id",
                table: "fixtures",
                column: "home_team_id",
                principalTable: "teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
