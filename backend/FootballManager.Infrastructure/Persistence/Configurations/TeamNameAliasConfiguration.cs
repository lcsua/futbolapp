using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations;

public class TeamNameAliasConfiguration : IEntityTypeConfiguration<TeamNameAlias>
{
    public void Configure(EntityTypeBuilder<TeamNameAlias> builder)
    {
        builder.ToTable("team_name_aliases");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.LeagueId).HasColumnName("league_id");
        builder.Property(e => e.TeamId).HasColumnName("team_id");

        builder.Property(e => e.Alias)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("alias");

        builder.Property(e => e.NormalizedAlias)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("normalized_alias");

        builder.Property(e => e.Source)
            .IsRequired()
            .HasMaxLength(40)
            .HasColumnName("source");

        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => new { e.LeagueId, e.NormalizedAlias })
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasOne(e => e.League)
            .WithMany()
            .HasForeignKey(e => e.LeagueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Team)
            .WithMany()
            .HasForeignKey(e => e.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
