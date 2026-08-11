using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations;

/// <inheritdoc />
[DbContext(typeof(FootballManagerDbContext))]
[Migration("20260811010000_PlayerRosterAndGoalAttribution")]
public partial class PlayerRosterAndGoalAttribution : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "nickname",
            table: "players",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "birth_date",
            table: "players",
            type: "date",
            nullable: true,
            oldClrType: typeof(DateOnly),
            oldType: "date");

        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_players_document";
            """);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX "IX_players_document"
            ON players (document)
            WHERE document IS NOT NULL AND document <> '';
            """);

        migrationBuilder.AddColumn<Guid>(
            name: "player_id",
            table: "match_incidents",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "against_player_id",
            table: "match_incidents",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_match_incidents_player_id",
            table: "match_incidents",
            column: "player_id");

        migrationBuilder.CreateIndex(
            name: "IX_match_incidents_against_player_id",
            table: "match_incidents",
            column: "against_player_id");

        migrationBuilder.AddForeignKey(
            name: "FK_match_incidents_players_player_id",
            table: "match_incidents",
            column: "player_id",
            principalTable: "players",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_match_incidents_players_against_player_id",
            table: "match_incidents",
            column: "against_player_id",
            principalTable: "players",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_match_incidents_players_player_id",
            table: "match_incidents");

        migrationBuilder.DropForeignKey(
            name: "FK_match_incidents_players_against_player_id",
            table: "match_incidents");

        migrationBuilder.DropIndex(
            name: "IX_match_incidents_player_id",
            table: "match_incidents");

        migrationBuilder.DropIndex(
            name: "IX_match_incidents_against_player_id",
            table: "match_incidents");

        migrationBuilder.DropColumn(
            name: "player_id",
            table: "match_incidents");

        migrationBuilder.DropColumn(
            name: "against_player_id",
            table: "match_incidents");

        migrationBuilder.DropColumn(
            name: "nickname",
            table: "players");

        migrationBuilder.AlterColumn<DateOnly>(
            name: "birth_date",
            table: "players",
            type: "date",
            nullable: false,
            defaultValue: new DateOnly(1900, 1, 1),
            oldClrType: typeof(DateOnly),
            oldType: "date",
            oldNullable: true);

        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_players_document";
            """);

        migrationBuilder.CreateIndex(
            name: "IX_players_document",
            table: "players",
            column: "document",
            unique: true);
    }
}
