using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations
{
    [Migration("20260209100000_AddLeagueIdToFields")]
    public partial class AddLeagueIdToFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "league_id",
                table: "fields",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE fields SET league_id = (SELECT id FROM leagues LIMIT 1) WHERE league_id IS NULL AND EXISTS (SELECT 1 FROM leagues LIMIT 1);");

            migrationBuilder.AlterColumn<Guid>(
                name: "league_id",
                table: "fields",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_fields_league_id_name",
                table: "fields",
                columns: new[] { "league_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_fields_leagues_league_id",
                table: "fields",
                column: "league_id",
                principalTable: "leagues",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fields_leagues_league_id",
                table: "fields");

            migrationBuilder.DropIndex(
                name: "IX_fields_league_id_name",
                table: "fields");

            migrationBuilder.DropColumn(
                name: "league_id",
                table: "fields");
        }
    }
}
