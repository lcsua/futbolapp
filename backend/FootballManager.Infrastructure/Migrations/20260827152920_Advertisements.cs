using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Advertisements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "advertisements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    advertiser_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    desktop_image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    mobile_image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    target_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    slot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_advertisements", x => x.id);
                    table.CheckConstraint("CK_advertisements_ends_at_gte_starts_at", "ends_at IS NULL OR starts_at IS NULL OR ends_at >= starts_at");
                    table.ForeignKey(
                        name: "FK_advertisements_leagues_league_id",
                        column: x => x.league_id,
                        principalTable: "leagues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_advertisements_ends_at",
                table: "advertisements",
                column: "ends_at");

            migrationBuilder.CreateIndex(
                name: "IX_advertisements_is_active",
                table: "advertisements",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_advertisements_league_id",
                table: "advertisements",
                column: "league_id");

            migrationBuilder.CreateIndex(
                name: "IX_advertisements_league_id_slot",
                table: "advertisements",
                columns: new[] { "league_id", "slot" });

            migrationBuilder.CreateIndex(
                name: "IX_advertisements_starts_at",
                table: "advertisements",
                column: "starts_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "advertisements");
        }
    }
}
