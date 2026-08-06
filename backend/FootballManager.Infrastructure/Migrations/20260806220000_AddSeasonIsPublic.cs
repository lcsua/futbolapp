using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations;

/// <inheritdoc />
[DbContext(typeof(FootballManagerDbContext))]
[Migration("20260806220000_AddSeasonIsPublic")]
public partial class AddSeasonIsPublic : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Existing seasons stay visible on public-web; new ones default false in domain.
        migrationBuilder.AddColumn<bool>(
            name: "is_public",
            table: "seasons",
            type: "boolean",
            nullable: false,
            defaultValue: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "is_public",
            table: "seasons");
    }
}
