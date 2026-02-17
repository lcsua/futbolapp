using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class SeasonConfiguration : IEntityTypeConfiguration<Season>
    {
        public void Configure(EntityTypeBuilder<Season> builder)
        {
            builder.ToTable("seasons");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.LeagueId).HasColumnName("league_id");

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");

            builder.Property(e => e.StartDate).HasColumnName("start_date");
            builder.Property(e => e.EndDate).HasColumnName("end_date");
            builder.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => new { e.LeagueId, e.Name }).IsUnique();

            builder.HasOne(e => e.League)
                .WithMany(l => l.Seasons)
                .HasForeignKey(e => e.LeagueId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(e => e.DivisionSeasons)
                .WithOne(e => e.Season)
                .HasForeignKey(e => e.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
