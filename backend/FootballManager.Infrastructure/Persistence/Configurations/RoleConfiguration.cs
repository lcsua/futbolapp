using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("roles");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.Name).IsRequired().HasMaxLength(50).HasColumnName("name");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.Code).HasMaxLength(30).HasColumnName("code");
            builder.Property(e => e.IsSystem).HasDefaultValue(false).HasColumnName("is_system");
            builder.Property(e => e.LeagueId).HasColumnName("league_id");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => e.Code)
                .IsUnique()
                .HasFilter("code IS NOT NULL");

            builder.HasIndex(e => new { e.LeagueId, e.Name })
                .IsUnique()
                .HasFilter("league_id IS NOT NULL");

            builder.HasIndex(e => e.Name)
                .IsUnique()
                .HasFilter("league_id IS NULL");

            builder.HasOne(e => e.League)
                .WithMany()
                .HasForeignKey(e => e.LeagueId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.RolePermissions)
                .WithOne(e => e.Role)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(e => e.RolePermissions).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
