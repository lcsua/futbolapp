using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class CompetitionMatchDayConfiguration : IEntityTypeConfiguration<CompetitionMatchDay>
    {
        public void Configure(EntityTypeBuilder<CompetitionMatchDay> builder)
        {
            builder.ToTable("competition_match_days");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.CompetitionRuleId).HasColumnName("competition_rule_id");
            builder.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasOne(e => e.CompetitionRule)
                .WithMany(c => c.MatchDays)
                .HasForeignKey(e => e.CompetitionRuleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
