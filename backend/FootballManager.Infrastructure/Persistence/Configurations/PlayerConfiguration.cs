using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class PlayerConfiguration : IEntityTypeConfiguration<Player>
    {
        public void Configure(EntityTypeBuilder<Player> builder)
        {
            builder.ToTable("players");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.TeamId).HasColumnName("team_id");

            builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100).HasColumnName("first_name");
            builder.Property(e => e.LastName).IsRequired().HasMaxLength(100).HasColumnName("last_name");
            builder.Property(e => e.Document).HasMaxLength(50).HasColumnName("document");
            builder.Property(e => e.BirthDate).HasColumnName("birth_date");
            builder.Property(e => e.JerseyNumber).HasColumnName("jersey_number");
            
            builder.Property(e => e.Position)
                .HasMaxLength(10)
                .HasColumnName("position")
                .HasConversion<string>(); // Map Enum to String

            builder.Property(e => e.Phone).HasMaxLength(50).HasColumnName("phone");
            builder.Property(e => e.Email).HasMaxLength(100).HasColumnName("email");
            builder.Property(e => e.Nationality).HasMaxLength(100).HasColumnName("nationality");
            builder.Property(e => e.HeightCm).HasColumnName("height_cm");
            builder.Property(e => e.WeightKg).HasColumnName("weight_kg");
            builder.Property(e => e.PhotoUrl).HasColumnName("photo_url");
            builder.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => new { e.TeamId, e.JerseyNumber }).IsUnique();
            builder.HasIndex(e => e.Document).IsUnique();
        }
    }
}
