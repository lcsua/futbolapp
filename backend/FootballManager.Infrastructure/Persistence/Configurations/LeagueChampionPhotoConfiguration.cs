using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class LeagueChampionPhotoConfiguration : IEntityTypeConfiguration<LeagueChampionPhoto>
    {
        public void Configure(EntityTypeBuilder<LeagueChampionPhoto> builder)
        {
            builder.ToTable("league_champion_photos", t =>
                t.HasCheckConstraint("CK_league_champion_photos_display_order", "display_order >= 0"));

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.LeagueChampionId).HasColumnName("league_champion_id");

            builder.Property(e => e.ImageUrl)
                .IsRequired()
                .HasMaxLength(LeagueChampionPhoto.MaxImageUrlLength)
                .HasColumnName("image_url");

            builder.Property(e => e.Caption)
                .HasMaxLength(LeagueChampionPhoto.MaxCaptionLength)
                .HasColumnName("caption");

            builder.Property(e => e.DisplayOrder).HasColumnName("display_order");
            builder.Property(e => e.IsCover).HasColumnName("is_cover");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => new { e.LeagueChampionId, e.DisplayOrder });
            builder.HasIndex(e => e.LeagueChampionId)
                .IsUnique()
                .HasFilter("is_cover = true AND deleted_at IS NULL")
                .HasDatabaseName("UX_league_champion_photos_one_cover");
        }
    }
}
