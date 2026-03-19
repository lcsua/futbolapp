using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlugsAndIsPublic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Leagues: add slug (nullable first) and is_public
            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "leagues",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_public",
                table: "leagues",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE leagues SET slug = trim(both '-' from lower(regexp_replace(regexp_replace(regexp_replace(
                    translate(name, 'áéíóúüñÁÉÍÓÚÜÑàèìòùÀÈÌÒÙ', 'aeiouunAEIOUUNaeiouAEIOU'),
                    '[^a-zA-Z0-9\s-]', '', 'g'), '\s+', '-', 'g'), '-+', '-', 'g')))
                WHERE slug IS NULL;
                UPDATE leagues SET slug = 'league-' || id::text WHERE slug IS NULL OR slug = '';
            ");

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "leagues",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_leagues_slug",
                table: "leagues",
                column: "slug",
                unique: true);

            // Teams: add slug (nullable first)
            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "teams",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.Sql(@"
                WITH base AS (
                    SELECT id, league_id, name,
                        trim(both '-' from lower(regexp_replace(regexp_replace(regexp_replace(
                            translate(name, 'áéíóúüñÁÉÍÓÚÜÑàèìòùÀÈÌÒÙ', 'aeiouunAEIOUUNaeiouAEIOU'),
                            '[^a-zA-Z0-9\s-]', '', 'g'), '\s+', '-', 'g'), '-+', '-', 'g'))) as base_slug
                    FROM teams
                ),
                ranked AS (
                    SELECT id, league_id, base_slug,
                        CASE WHEN base_slug = '' OR base_slug IS NULL THEN 'team-' || id::text ELSE base_slug END as clean_slug,
                        row_number() OVER (PARTITION BY league_id, COALESCE(NULLIF(base_slug,''), 'team-' || id::text) ORDER BY id) as rn
                    FROM base
                )
                UPDATE teams t SET slug = CASE
                    WHEN r.rn = 1 THEN r.clean_slug
                    ELSE r.clean_slug || '-' || (r.rn - 1)::text
                END
                FROM ranked r WHERE t.id = r.id;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "teams",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_league_id_slug",
                table: "teams",
                columns: new[] { "league_id", "slug" },
                unique: true);

            // Divisions: add slug (nullable first)
            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "divisions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.Sql(@"
                WITH base AS (
                    SELECT id, league_id, name,
                        trim(both '-' from lower(regexp_replace(regexp_replace(regexp_replace(
                            translate(name, 'áéíóúüñÁÉÍÓÚÜÑàèìòùÀÈÌÒÙ', 'aeiouunAEIOUUNaeiouAEIOU'),
                            '[^a-zA-Z0-9\s-]', '', 'g'), '\s+', '-', 'g'), '-+', '-', 'g'))) as base_slug
                    FROM divisions
                ),
                ranked AS (
                    SELECT id, league_id, base_slug,
                        CASE WHEN base_slug = '' OR base_slug IS NULL THEN 'division-' || id::text ELSE base_slug END as clean_slug,
                        row_number() OVER (PARTITION BY league_id, COALESCE(NULLIF(base_slug,''), 'division-' || id::text) ORDER BY id) as rn
                    FROM base
                )
                UPDATE divisions d SET slug = CASE
                    WHEN r.rn = 1 THEN r.clean_slug
                    ELSE r.clean_slug || '-' || (r.rn - 1)::text
                END
                FROM ranked r WHERE d.id = r.id;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "divisions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_divisions_league_id_slug",
                table: "divisions",
                columns: new[] { "league_id", "slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_teams_league_id_slug",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_leagues_slug",
                table: "leagues");

            migrationBuilder.DropIndex(
                name: "IX_divisions_league_id_slug",
                table: "divisions");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "is_public",
                table: "leagues");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "leagues");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "divisions");
        }
    }
}
