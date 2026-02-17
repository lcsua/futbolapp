using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class CompetitionRuleConfiguration : IEntityTypeConfiguration<CompetitionRule>
    {
        public void Configure(EntityTypeBuilder<CompetitionRule> builder)
        {
            builder.ToTable("competition_rules");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.LeagueId).HasColumnName("league_id");
            builder.Property(e => e.SeasonId).HasColumnName("season_id");
            builder.Property(e => e.MatchesPerWeek).HasDefaultValue(1).HasColumnName("matches_per_week");
            builder.Property(e => e.IsHomeAway).HasDefaultValue(false).HasColumnName("is_home_away");
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasOne(e => e.League)
                .WithMany()
                .HasForeignKey(e => e.LeagueId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.Season)
                .WithMany()
                .HasForeignKey(e => e.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
