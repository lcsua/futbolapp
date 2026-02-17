using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class MatchEventConfiguration : IEntityTypeConfiguration<MatchEvent>
    {
        public void Configure(EntityTypeBuilder<MatchEvent> builder)
        {
            builder.ToTable("match_events");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.FixtureId).HasColumnName("fixture_id");
            builder.Property(e => e.PlayerId).HasColumnName("player_id");

            builder.Property(e => e.EventType)
                .HasMaxLength(20)
                .HasColumnName("event_type")
                .HasConversion<string>();

            builder.Property(e => e.Minute).HasColumnName("minute");
            builder.Property(e => e.ExtraInfo).HasColumnName("extra_info");
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");

            builder.HasOne(e => e.Fixture)
                .WithMany(e => e.Events)
                .HasForeignKey(e => e.FixtureId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
