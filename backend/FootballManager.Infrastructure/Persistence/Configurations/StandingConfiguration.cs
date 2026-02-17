using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class StandingConfiguration : IEntityTypeConfiguration<Standing>
    {
        public void Configure(EntityTypeBuilder<Standing> builder)
        {
            builder.ToView("standings");
            builder.HasNoKey();

            builder.Property(e => e.TeamId).HasColumnName("team_id");
            builder.Property(e => e.TeamName).HasColumnName("team_name");
            builder.Property(e => e.LogoUrl).HasColumnName("logo_url");
            builder.Property(e => e.DivisionName).HasColumnName("division_name");
            builder.Property(e => e.SeasonName).HasColumnName("season_name");
            builder.Property(e => e.LeagueName).HasColumnName("league_name");
            builder.Property(e => e.MatchesPlayed).HasColumnName("matches_played");
            builder.Property(e => e.Wins).HasColumnName("wins");
            builder.Property(e => e.Draws).HasColumnName("draws");
            builder.Property(e => e.Losses).HasColumnName("losses");
            builder.Property(e => e.Points).HasColumnName("points");
            builder.Property(e => e.GoalsFor).HasColumnName("goals_for");
            builder.Property(e => e.GoalsAgainst).HasColumnName("goals_against");
            builder.Property(e => e.GoalDifference).HasColumnName("goal_difference");
        }
    }
}
