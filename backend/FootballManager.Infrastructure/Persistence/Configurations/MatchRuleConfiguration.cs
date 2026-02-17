using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class MatchRuleConfiguration : IEntityTypeConfiguration<MatchRule>
    {
        public void Configure(EntityTypeBuilder<MatchRule> builder)
        {
            builder.ToTable("match_rules");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.LeagueId).HasColumnName("league_id");
            builder.Property(e => e.SeasonId).HasColumnName("season_id");
            builder.Property(e => e.HalfMinutes).HasColumnName("half_minutes");
            builder.Property(e => e.BreakMinutes).HasColumnName("break_minutes");
            builder.Property(e => e.WarmupBufferMinutes).HasDefaultValue(0).HasColumnName("warmup_buffer_minutes");
            builder.Property(e => e.SlotGranularityMinutes).HasDefaultValue(5).HasColumnName("slot_granularity_minutes");
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
