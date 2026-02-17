using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class FieldConfiguration : IEntityTypeConfiguration<Field>
    {
        public void Configure(EntityTypeBuilder<Field> builder)
        {
            builder.ToTable("fields");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.LeagueId).HasColumnName("league_id");

            builder.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            builder.Property(e => e.Address).HasColumnName("address");
            builder.Property(e => e.City).HasMaxLength(100).HasColumnName("city");
            builder.Property(e => e.GeoLat).HasColumnName("geo_lat");
            builder.Property(e => e.GeoLng).HasColumnName("geo_lng");
            builder.Property(e => e.IsAvailable).HasDefaultValue(true).HasColumnName("is_available");
            builder.Property(e => e.Description).HasColumnName("description");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => new { e.LeagueId, e.Name }).IsUnique();
            builder.HasOne(e => e.League)
                .WithMany(l => l.Fields)
                .HasForeignKey(e => e.LeagueId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
