using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MatchPredictionForecasts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "match_prediction_forecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeagueSlug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SeasonSlug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DivisionSlug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    Kickoff = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HomeTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    AwayTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    HomeWin = table.Column<double>(type: "double precision", nullable: false),
                    Draw = table.Column<double>(type: "double precision", nullable: false),
                    AwayWin = table.Column<double>(type: "double precision", nullable: false),
                    LikelyHomeGoals = table.Column<int>(type: "integer", nullable: false),
                    LikelyAwayGoals = table.Column<int>(type: "integer", nullable: false),
                    ExpectedHomeGoals = table.Column<double>(type: "double precision", nullable: false),
                    ExpectedAwayGoals = table.Column<double>(type: "double precision", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_prediction_forecasts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_match_prediction_forecasts_LeagueSlug_SeasonSlug_DivisionSl~",
                table: "match_prediction_forecasts",
                columns: new[] { "LeagueSlug", "SeasonSlug", "DivisionSlug", "Round" });

            migrationBuilder.CreateIndex(
                name: "IX_match_prediction_forecasts_MatchId_ModelVersion",
                table: "match_prediction_forecasts",
                columns: new[] { "MatchId", "ModelVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "match_prediction_forecasts");
        }
    }
}
