using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FootballManager.Domain.Enums;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.FullName).IsRequired().HasMaxLength(150).HasColumnName("full_name");
            builder.Property(e => e.Email).IsRequired().HasMaxLength(150).HasColumnName("email");
            builder.Property(e => e.PasswordHash).IsRequired(false).HasColumnName("password_hash");
            builder.Property(e => e.GoogleSub).IsRequired(false).HasMaxLength(255).HasColumnName("google_sub");
            builder.Property(e => e.AvatarUrl).IsRequired(false).HasColumnName("avatar_url");
            
            builder.Property(e => e.Role)
                .HasMaxLength(30)
                .HasDefaultValue(UserRole.ADMIN) // Default in SQL is 'ADMIN', strange but ok
                .HasColumnName("role")
                .HasConversion<string>();

            builder.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            builder.Property(e => e.IsVerified).HasDefaultValue(false).HasColumnName("is_verified");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(e => e.Email).IsUnique();
        }
    }
}
