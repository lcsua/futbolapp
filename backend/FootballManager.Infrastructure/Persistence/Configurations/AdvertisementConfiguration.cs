using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations;

public class AdvertisementConfiguration : IEntityTypeConfiguration<Advertisement>
{
    public void Configure(EntityTypeBuilder<Advertisement> builder)
    {
        builder.ToTable("advertisements", t => t.HasCheckConstraint(
            "CK_advertisements_ends_at_gte_starts_at",
            "ends_at IS NULL OR starts_at IS NULL OR ends_at >= starts_at"));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.LeagueId).HasColumnName("league_id");

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(e => e.AdvertiserName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("advertiser_name");

        builder.Property(e => e.DesktopImageUrl)
            .HasMaxLength(1000)
            .HasColumnName("desktop_image_url");

        builder.Property(e => e.MobileImageUrl)
            .HasMaxLength(1000)
            .HasColumnName("mobile_image_url");

        builder.Property(e => e.TargetUrl)
            .HasMaxLength(1000)
            .HasColumnName("target_url");

        builder.Property(e => e.Slot)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("slot")
            .HasConversion<string>();

        builder.Property(e => e.StartsAt).HasColumnName("starts_at");
        builder.Property(e => e.EndsAt).HasColumnName("ends_at");

        builder.Property(e => e.Priority)
            .HasDefaultValue(0)
            .HasColumnName("priority");

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.LeagueId);
        builder.HasIndex(e => new { e.LeagueId, e.Slot });
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.StartsAt);
        builder.HasIndex(e => e.EndsAt);

        builder.HasOne(e => e.League)
            .WithMany()
            .HasForeignKey(e => e.LeagueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
