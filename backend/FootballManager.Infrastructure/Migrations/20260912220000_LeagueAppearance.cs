using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations;

[DbContext(typeof(FootballManagerDbContext))]
[Migration("20260912220000_LeagueAppearance")]
public partial class AddLeagueAppearance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "primary_color",
            table: "leagues",
            type: "character varying(16)",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "font_key",
            table: "leagues",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "hero_image_url",
            table: "leagues",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "team_hero_image_url",
            table: "leagues",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "primary_color", table: "leagues");
        migrationBuilder.DropColumn(name: "font_key", table: "leagues");
        migrationBuilder.DropColumn(name: "hero_image_url", table: "leagues");
        migrationBuilder.DropColumn(name: "team_hero_image_url", table: "leagues");
    }
}
