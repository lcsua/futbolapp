using System;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations;

[DbContext(typeof(FootballManagerDbContext))]
[Migration("20260811030000_LeagueDocuments")]
public partial class LeagueDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "league_document_categories",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                league_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                requires_document_date = table.Column<bool>(type: "boolean", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_league_document_categories", x => x.id);
                table.ForeignKey(
                    name: "FK_league_document_categories_leagues_league_id",
                    column: x => x.league_id,
                    principalTable: "leagues",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "league_documents",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                league_id = table.Column<Guid>(type: "uuid", nullable: false),
                category_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                relative_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                original_file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                document_date = table.Column<DateOnly>(type: "date", nullable: true),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_league_documents", x => x.id);
                table.ForeignKey(
                    name: "FK_league_documents_leagues_league_id",
                    column: x => x.league_id,
                    principalTable: "leagues",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_league_documents_league_document_categories_category_id",
                    column: x => x.category_id,
                    principalTable: "league_document_categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_league_document_categories_league_id_slug",
            table: "league_document_categories",
            columns: new[] { "league_id", "slug" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_league_documents_league_id_category_id_sort_order",
            table: "league_documents",
            columns: new[] { "league_id", "category_id", "sort_order" });

        migrationBuilder.CreateIndex(
            name: "IX_league_documents_category_id",
            table: "league_documents",
            column: "category_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "league_documents");
        migrationBuilder.DropTable(name: "league_document_categories");
    }
}
