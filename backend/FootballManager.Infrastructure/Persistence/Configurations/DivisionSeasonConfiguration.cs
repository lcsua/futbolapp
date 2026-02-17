using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class DivisionSeasonConfiguration : IEntityTypeConfiguration<DivisionSeason>
    {
        public void Configure(EntityTypeBuilder<DivisionSeason> builder)
        {
            builder.ToTable("division_seasons");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.SeasonId).HasColumnName("season_id");
            builder.Property(e => e.DivisionId).HasColumnName("division_id");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => new { e.SeasonId, e.DivisionId }).IsUnique();

            builder.HasOne(e => e.Season)
                .WithMany(s => s.DivisionSeasons)
                .HasForeignKey(e => e.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Division)
                .WithMany()
                .HasForeignKey(e => e.DivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(e => e.TeamAssignments)
                .WithOne(t => t.DivisionSeason)
                .HasForeignKey(e => e.DivisionSeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
