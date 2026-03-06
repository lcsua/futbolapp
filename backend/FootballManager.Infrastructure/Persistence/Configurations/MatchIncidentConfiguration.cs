using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class MatchIncidentConfiguration : IEntityTypeConfiguration<MatchIncident>
    {
        public void Configure(EntityTypeBuilder<MatchIncident> builder)
        {
            builder.ToTable("match_incidents");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.FixtureId).HasColumnName("match_id");
            builder.Property(e => e.Minute).HasColumnName("minute");
            builder.Property(e => e.TeamId).HasColumnName("team_id");
            builder.Property(e => e.PlayerName).HasMaxLength(200).HasColumnName("player_name");
            builder.Property(e => e.IncidentType)
                .HasMaxLength(20)
                .HasColumnName("incident_type")
                .HasConversion<string>();
            builder.Property(e => e.Notes).HasMaxLength(500).HasColumnName("notes");
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");

            builder.HasIndex(e => e.FixtureId);

            builder.HasOne(e => e.Fixture)
                .WithMany(f => f.Incidents)
                .HasForeignKey(e => e.FixtureId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(e => e.Team)
                .WithMany()
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
