using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations;

[DbContext(typeof(FootballManagerDbContext))]
[Migration("20260811020000_MatchIncidentMinuteOptional")]
public partial class MatchIncidentMinuteOptional : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "minute",
            table: "match_incidents",
            type: "integer",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE match_incidents SET minute = 0 WHERE minute IS NULL;
            """);

        migrationBuilder.AlterColumn<int>(
            name: "minute",
            table: "match_incidents",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);
    }
}
