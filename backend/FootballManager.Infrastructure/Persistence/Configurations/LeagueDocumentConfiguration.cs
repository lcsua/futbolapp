using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class LeagueDocumentConfiguration : IEntityTypeConfiguration<LeagueDocument>
    {
        public void Configure(EntityTypeBuilder<LeagueDocument> builder)
        {
            builder.ToTable("league_documents");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.LeagueId).HasColumnName("league_id");
            builder.Property(e => e.CategoryId).HasColumnName("category_id");

            builder.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(300)
                .HasColumnName("title");

            builder.Property(e => e.Description)
                .HasMaxLength(2000)
                .HasColumnName("description");

            builder.Property(e => e.FileUrl)
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnName("file_url");

            builder.Property(e => e.RelativePath)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("relative_path");

            builder.Property(e => e.OriginalFileName)
                .IsRequired()
                .HasMaxLength(300)
                .HasColumnName("original_file_name");

            builder.Property(e => e.ContentType)
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnName("content_type");

            builder.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
            builder.Property(e => e.DocumentDate).HasColumnName("document_date");
            builder.Property(e => e.SortOrder).HasColumnName("sort_order");
            builder.Property(e => e.IsPublished).HasColumnName("is_published");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => new { e.LeagueId, e.CategoryId, e.SortOrder });

            builder.HasOne(e => e.League)
                .WithMany()
                .HasForeignKey(e => e.LeagueId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
