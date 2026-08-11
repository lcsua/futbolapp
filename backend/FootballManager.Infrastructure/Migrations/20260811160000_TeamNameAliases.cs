using System;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations;

[DbContext(typeof(FootballManagerDbContext))]
[Migration("20260811160000_TeamNameAliases")]
public partial class TeamNameAliases : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "team_name_aliases",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                league_id = table.Column<Guid>(type: "uuid", nullable: false),
                team_id = table.Column<Guid>(type: "uuid", nullable: false),
                alias = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                normalized_alias = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_team_name_aliases", x => x.id);
                table.ForeignKey(
                    name: "FK_team_name_aliases_leagues_league_id",
                    column: x => x.league_id,
                    principalTable: "leagues",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_team_name_aliases_teams_team_id",
                    column: x => x.team_id,
                    principalTable: "teams",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_team_name_aliases_league_id_normalized_alias",
            table: "team_name_aliases",
            columns: new[] { "league_id", "normalized_alias" },
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_team_name_aliases_team_id",
            table: "team_name_aliases",
            column: "team_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "team_name_aliases");
    }
}
