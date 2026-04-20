using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class DivisionConfiguration : IEntityTypeConfiguration<Division>
    {
        public void Configure(EntityTypeBuilder<Division> builder)
        {
            builder.ToTable("divisions");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.LeagueId).HasColumnName("league_id");

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("name");

            builder.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnName("slug");

            builder.HasIndex(e => new { e.LeagueId, e.Slug }).IsUnique();

            builder.Property(e => e.Description).HasColumnName("description");

            builder.Property(e => e.KickoffRestrictionEnabled)
                .HasDefaultValue(false)
                .HasColumnName("kickoff_restriction_enabled");

            builder.Property(e => e.KickoffRestrictionStart).HasColumnName("kickoff_restriction_start");
            builder.Property(e => e.KickoffRestrictionEnd).HasColumnName("kickoff_restriction_end");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => new { e.LeagueId, e.Name }).IsUnique();

            builder.HasOne(e => e.League)
                .WithMany(l => l.Divisions)
                .HasForeignKey(e => e.LeagueId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
