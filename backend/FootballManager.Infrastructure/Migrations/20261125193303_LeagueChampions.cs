using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LeagueChampions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "league_champions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    championship_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    division_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    identity_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    division_id = table.Column<Guid>(type: "uuid", nullable: true),
                    won_at = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_champions", x => x.id);
                    table.CheckConstraint("CK_league_champions_season_requires_division", "season_id IS NULL OR division_id IS NOT NULL");
                    table.CheckConstraint("CK_league_champions_year", "year >= 1900 AND year <= 2100");
                    table.ForeignKey(
                        name: "FK_league_champions_divisions_division_id",
                        column: x => x.division_id,
                        principalTable: "divisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_league_champions_leagues_league_id",
                        column: x => x.league_id,
                        principalTable: "leagues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_league_champions_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_league_champions_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "league_champion_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_champion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    caption = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_cover = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_champion_photos", x => x.id);
                    table.CheckConstraint("CK_league_champion_photos_display_order", "display_order >= 0");
                    table.ForeignKey(
                        name: "FK_league_champion_photos_league_champions_league_champion_id",
                        column: x => x.league_champion_id,
                        principalTable: "league_champions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_league_champion_photos_league_champion_id_display_order",
                table: "league_champion_photos",
                columns: new[] { "league_champion_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "UX_league_champion_photos_one_cover",
                table: "league_champion_photos",
                column: "league_champion_id",
                unique: true,
                filter: "is_cover = true AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_league_champions_division_id",
                table: "league_champions",
                column: "division_id");

            migrationBuilder.CreateIndex(
                name: "IX_league_champions_league_id",
                table: "league_champions",
                column: "league_id");

            migrationBuilder.CreateIndex(
                name: "IX_league_champions_season_id",
                table: "league_champions",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_league_champions_team_id",
                table: "league_champions",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_league_champions_year",
                table: "league_champions",
                column: "year");

            migrationBuilder.CreateIndex(
                name: "UX_league_champions_identity",
                table: "league_champions",
                columns: new[] { "league_id", "identity_key" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "league_champion_photos");

            migrationBuilder.DropTable(
                name: "league_champions");
        }
    }
}
