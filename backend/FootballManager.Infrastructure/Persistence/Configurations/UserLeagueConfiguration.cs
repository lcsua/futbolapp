using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FootballManager.Domain.Enums;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class UserLeagueConfiguration : IEntityTypeConfiguration<UserLeague>
    {
        public void Configure(EntityTypeBuilder<UserLeague> builder)
        {
            builder.ToTable("user_leagues");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.UserId).HasColumnName("user_id");
            builder.Property(e => e.LeagueId).HasColumnName("league_id");

            builder.Property(e => e.AssignedRole)
                .HasMaxLength(30)
                .HasDefaultValue(UserRole.ADMIN)
                .HasColumnName("assigned_role")
                .HasConversion<string>();

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");

            builder.HasIndex(e => new { e.UserId, e.LeagueId }).IsUnique();

            builder.HasOne(e => e.User)
                .WithMany(e => e.UserLeagues)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.League)
                .WithMany(e => e.UserLeagues)
                .HasForeignKey(e => e.LeagueId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
