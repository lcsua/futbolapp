using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class LeagueDocumentCategoryConfiguration : IEntityTypeConfiguration<LeagueDocumentCategory>
    {
        public void Configure(EntityTypeBuilder<LeagueDocumentCategory> builder)
        {
            builder.ToTable("league_document_categories");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.LeagueId).HasColumnName("league_id");

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            builder.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("slug");

            builder.Property(e => e.SortOrder).HasColumnName("sort_order");
            builder.Property(e => e.RequiresDocumentDate).HasColumnName("requires_document_date");
            builder.Property(e => e.IsActive).HasColumnName("is_active");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => new { e.LeagueId, e.Slug }).IsUnique();

            builder.HasOne(e => e.League)
                .WithMany()
                .HasForeignKey(e => e.LeagueId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
