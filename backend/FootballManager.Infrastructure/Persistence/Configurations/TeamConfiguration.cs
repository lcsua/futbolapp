using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class TeamConfiguration : IEntityTypeConfiguration<Team>
    {
        public void Configure(EntityTypeBuilder<Team> builder)
        {
            builder.ToTable("teams");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.LeagueId).HasColumnName("league_id");

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");

            builder.Property(e => e.ShortName).HasMaxLength(20).HasColumnName("short_name");
            builder.Property(e => e.PrimaryColor).HasMaxLength(50).HasColumnName("primary_color");
            builder.Property(e => e.SecondaryColor).HasMaxLength(50).HasColumnName("secondary_color");
            builder.Property(e => e.FoundedYear).HasColumnName("founded_year");
            builder.Property(e => e.DelegateName).HasMaxLength(100).HasColumnName("delegate_name");
            builder.Property(e => e.DelegateContact).HasMaxLength(100).HasColumnName("delegate_contact");
            builder.Property(e => e.Email).HasMaxLength(150).HasColumnName("email");
            builder.Property(e => e.LogoUrl).HasColumnName("logo_url");
            builder.Property(e => e.PhotoUrl).HasColumnName("photo_url");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => new { e.LeagueId, e.Name }).IsUnique();

            builder.HasOne(e => e.League)
                .WithMany(l => l.Teams)
                .HasForeignKey(e => e.LeagueId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(e => e.Players)
                .WithOne(e => e.Team)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
