using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class TeamDivisionSeasonConfiguration : IEntityTypeConfiguration<TeamDivisionSeason>
    {
        public void Configure(EntityTypeBuilder<TeamDivisionSeason> builder)
        {
            builder.ToTable("team_division_seasons");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.TeamId).HasColumnName("team_id");
            builder.Property(e => e.DivisionSeasonId).HasColumnName("division_season_id");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => new { e.TeamId, e.DivisionSeasonId }).IsUnique();

            builder.HasOne(e => e.Team)
                .WithMany()
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.DivisionSeason)
                .WithMany(ds => ds.TeamAssignments)
                .HasForeignKey(e => e.DivisionSeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
