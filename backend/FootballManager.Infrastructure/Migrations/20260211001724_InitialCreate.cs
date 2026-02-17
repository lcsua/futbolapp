using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fields",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    geo_lat = table.Column<double>(type: "double precision", nullable: true),
                    geo_lng = table.Column<double>(type: "double precision", nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fields", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leagues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    logo_url = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leagues", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    google_sub = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "ADMIN"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "divisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_divisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_divisions_leagues_league_id",
                        column: x => x.league_id,
                        principalTable: "leagues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seasons", x => x.id);
                    table.ForeignKey(
                        name: "FK_seasons_leagues_league_id",
                        column: x => x.league_id,
                        principalTable: "leagues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    short_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    primary_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    secondary_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    founded_year = table.Column<int>(type: "integer", nullable: true),
                    delegate_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    delegate_contact = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    logo_url = table.Column<string>(type: "text", nullable: false),
                    photo_url = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_leagues_league_id",
                        column: x => x.league_id,
                        principalTable: "leagues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_leagues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "ADMIN"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_leagues", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_leagues_leagues_league_id",
                        column: x => x.league_id,
                        principalTable: "leagues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_leagues_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "division_seasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    division_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_division_seasons", x => x.id);
                    table.ForeignKey(
                        name: "FK_division_seasons_divisions_division_id",
                        column: x => x.division_id,
                        principalTable: "divisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_division_seasons_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    document = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    jersey_number = table.Column<int>(type: "integer", nullable: true),
                    position = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    height_cm = table.Column<int>(type: "integer", nullable: true),
                    weight_kg = table.Column<int>(type: "integer", nullable: true),
                    photo_url = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.id);
                    table.ForeignKey(
                        name: "FK_players_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fixtures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    division_season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    home_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    away_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_id = table.Column<Guid>(type: "uuid", nullable: true),
                    match_date = table.Column<DateOnly>(type: "date", nullable: false),
                    match_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    referee_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    round_number = table.Column<int>(type: "integer", nullable: true),
                    attendance = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fixtures", x => x.id);
                    table.ForeignKey(
                        name: "FK_fixtures_division_seasons_division_season_id",
                        column: x => x.division_season_id,
                        principalTable: "division_seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fixtures_fields_field_id",
                        column: x => x.field_id,
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_fixtures_teams_away_team_id",
                        column: x => x.away_team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fixtures_teams_home_team_id",
                        column: x => x.home_team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "team_division_seasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    division_season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_division_seasons", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_division_seasons_division_seasons_division_season_id",
                        column: x => x.division_season_id,
                        principalTable: "division_seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_team_division_seasons_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "match_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fixture_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    minute = table.Column<int>(type: "integer", nullable: true),
                    extra_info = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_match_events_fixtures_fixture_id",
                        column: x => x.fixture_id,
                        principalTable: "fixtures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_match_events_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fixture_id = table.Column<Guid>(type: "uuid", nullable: false),
                    home_team_goals = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    away_team_goals = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    notes = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_results_fixtures_fixture_id",
                        column: x => x.fixture_id,
                        principalTable: "fixtures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_division_seasons_division_id",
                table: "division_seasons",
                column: "division_id");

            migrationBuilder.CreateIndex(
                name: "IX_division_seasons_season_id_division_id",
                table: "division_seasons",
                columns: new[] { "season_id", "division_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_divisions_league_id_name",
                table: "divisions",
                columns: new[] { "league_id", "name" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_fixtures_home_team_id",
                table: "fixtures",
                column: "home_team_id");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_name",
                table: "leagues",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_match_events_fixture_id",
                table: "match_events",
                column: "fixture_id");

            migrationBuilder.CreateIndex(
                name: "IX_match_events_player_id",
                table: "match_events",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_players_document",
                table: "players",
                column: "document",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_players_team_id_jersey_number",
                table: "players",
                columns: new[] { "team_id", "jersey_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_results_fixture_id",
                table: "results",
                column: "fixture_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seasons_league_id_name",
                table: "seasons",
                columns: new[] { "league_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_division_seasons_division_season_id",
                table: "team_division_seasons",
                column: "division_season_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_division_seasons_team_id_division_season_id",
                table: "team_division_seasons",
                columns: new[] { "team_id", "division_season_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_league_id_name",
                table: "teams",
                columns: new[] { "league_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_leagues_league_id",
                table: "user_leagues",
                column: "league_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_leagues_user_id_league_id",
                table: "user_leagues",
                columns: new[] { "user_id", "league_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "match_events");

            migrationBuilder.DropTable(
                name: "results");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "team_division_seasons");

            migrationBuilder.DropTable(
                name: "user_leagues");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropTable(
                name: "fixtures");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "division_seasons");

            migrationBuilder.DropTable(
                name: "fields");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "divisions");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropTable(
                name: "leagues");
        }
    }
}
