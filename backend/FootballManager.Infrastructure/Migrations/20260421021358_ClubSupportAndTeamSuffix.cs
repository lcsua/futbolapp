using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClubSupportAndTeamSuffix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_teams_league_id_name",
                table: "teams");

            migrationBuilder.AddColumn<Guid>(
                name: "club_id",
                table: "teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suffix",
                table: "teams",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "clubs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clubs", x => x.id);
                    table.ForeignKey(
                        name: "FK_clubs_leagues_league_id",
                        column: x => x.league_id,
                        principalTable: "leagues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teams_club_id",
                table: "teams",
                column: "club_id");

            migrationBuilder.CreateIndex(
                name: "IX_clubs_league_id_name",
                table: "clubs",
                columns: new[] { "league_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_teams_clubs_club_id",
                table: "teams",
                column: "club_id",
                principalTable: "clubs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teams_clubs_club_id",
                table: "teams");

            migrationBuilder.DropTable(
                name: "clubs");

            migrationBuilder.DropIndex(
                name: "IX_teams_club_id",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "club_id",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "suffix",
                table: "teams");

            migrationBuilder.CreateIndex(
                name: "IX_teams_league_id_name",
                table: "teams",
                columns: new[] { "league_id", "name" },
                unique: true);
        }
    }
}
