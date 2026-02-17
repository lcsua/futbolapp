using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations
{
    [Migration("20260218120000_AddFirstMatchToleranceToMatchRules")]
    public partial class AddFirstMatchToleranceToMatchRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "first_match_tolerance_minutes",
                table: "match_rules",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "first_match_tolerance_minutes",
                table: "match_rules");
        }
    }
}
