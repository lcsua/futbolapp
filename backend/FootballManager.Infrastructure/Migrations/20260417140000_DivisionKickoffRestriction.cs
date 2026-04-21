using System;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace FootballManager.Infrastructure.Migrations
{
    [DbContext(typeof(FootballManagerDbContext))]
    [Migration("20260417140000_DivisionKickoffRestriction")]
    public partial class DivisionKickoffRestriction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "kickoff_restriction_enabled",
                table: "divisions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "kickoff_restriction_start",
                table: "divisions",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "kickoff_restriction_end",
                table: "divisions",
                type: "time without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "kickoff_restriction_enabled",
                table: "divisions");

            migrationBuilder.DropColumn(
                name: "kickoff_restriction_start",
                table: "divisions");

            migrationBuilder.DropColumn(
                name: "kickoff_restriction_end",
                table: "divisions");
        }
    }
}
