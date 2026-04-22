using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations;

public class DivisionSeasonFieldConfiguration : IEntityTypeConfiguration<DivisionSeasonField>
{
    public void Configure(EntityTypeBuilder<DivisionSeasonField> builder)
    {
        builder.ToTable("division_season_fields");

        builder.HasKey(e => new { e.DivisionSeasonId, e.FieldId });
        builder.Property(e => e.DivisionSeasonId).HasColumnName("division_season_id");
        builder.Property(e => e.FieldId).HasColumnName("field_id");

        builder.HasOne(e => e.DivisionSeason)
            .WithMany()
            .HasForeignKey(e => e.DivisionSeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Field)
            .WithMany()
            .HasForeignKey(e => e.FieldId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
