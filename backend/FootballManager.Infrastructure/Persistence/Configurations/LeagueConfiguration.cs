using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class LeagueConfiguration : IEntityTypeConfiguration<League>
    {
        public void Configure(EntityTypeBuilder<League> builder)
        {
            builder.ToTable("leagues");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");

            builder.HasIndex(e => e.Name).IsUnique();

            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.Country).HasMaxLength(100).HasColumnName("country");
            builder.Property(e => e.LogoUrl).HasColumnName("logo_url");
            builder.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasMany(e => e.Seasons)
                .WithOne(e => e.League)
                .HasForeignKey(e => e.LeagueId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
