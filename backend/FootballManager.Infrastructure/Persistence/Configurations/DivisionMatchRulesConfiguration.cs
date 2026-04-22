using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations;

public class DivisionMatchRulesConfiguration : IEntityTypeConfiguration<DivisionMatchRules>
{
    public void Configure(EntityTypeBuilder<DivisionMatchRules> builder)
    {
        builder.ToTable("division_match_rules");

        builder.HasKey(e => e.DivisionSeasonId);
        builder.Property(e => e.DivisionSeasonId).HasColumnName("division_season_id");
        builder.Property(e => e.HalfMinutes).HasColumnName("half_minutes");
        builder.Property(e => e.BreakMinutes).HasColumnName("halftime_break_minutes");
        builder.Property(e => e.WarmupBufferMinutes).HasColumnName("warmup_buffer_minutes");
        builder.Property(e => e.SlotGranularityMinutes).HasColumnName("slot_granularity_minutes");
        builder.Property(e => e.FirstMatchToleranceMinutes).HasColumnName("first_match_tolerance_minutes");
        builder.Property(e => e.BreakBetweenMatchesMinutes).HasColumnName("break_between_matches_minutes");
        builder.Property(e => e.AllowedTimeRangesJson).HasColumnName("allowed_time_ranges_json");

        builder.HasOne(e => e.DivisionSeason)
            .WithMany()
            .HasForeignKey(e => e.DivisionSeasonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
