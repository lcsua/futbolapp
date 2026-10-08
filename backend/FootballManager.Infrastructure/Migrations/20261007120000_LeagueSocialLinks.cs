using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations;

[DbContext(typeof(FootballManagerDbContext))]
[Migration("20261007120000_LeagueSocialLinks")]
public partial class LeagueSocialLinks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("facebook_url", "leagues", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>("instagram_url", "leagues", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>("youtube_url", "leagues", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>("tiktok_url", "leagues", type: "character varying(500)", maxLength: 500, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("facebook_url", "leagues");
        migrationBuilder.DropColumn("instagram_url", "leagues");
        migrationBuilder.DropColumn("youtube_url", "leagues");
        migrationBuilder.DropColumn("tiktok_url", "leagues");
    }
}
