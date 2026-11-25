using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class LeagueChampionConfiguration : IEntityTypeConfiguration<LeagueChampion>
    {
        public void Configure(EntityTypeBuilder<LeagueChampion> builder)
        {
            builder.ToTable("league_champions", t =>
            {
                t.HasCheckConstraint("CK_league_champions_year", "year >= 1900 AND year <= 2100");
                t.HasCheckConstraint(
                    "CK_league_champions_season_requires_division",
                    "season_id IS NULL OR division_id IS NOT NULL");
            });

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.LeagueId).HasColumnName("league_id");
            builder.Property(e => e.TeamId).HasColumnName("team_id");

            builder.Property(e => e.Year).HasColumnName("year");

            builder.Property(e => e.ChampionshipName)
                .IsRequired()
                .HasMaxLength(LeagueChampion.MaxChampionshipNameLength)
                .HasColumnName("championship_name");

            builder.Property(e => e.DivisionName)
                .IsRequired()
                .HasMaxLength(LeagueChampion.MaxDivisionNameLength)
                .HasColumnName("division_name");

            builder.Property(e => e.IdentityKey)
                .IsRequired()
                .HasMaxLength(300)
                .HasColumnName("identity_key");

            builder.Property(e => e.SeasonId).HasColumnName("season_id");
            builder.Property(e => e.DivisionId).HasColumnName("division_id");
            builder.Property(e => e.WonAt).HasColumnName("won_at").HasColumnType("date");

            builder.Property(e => e.Notes)
                .HasMaxLength(LeagueChampion.MaxNotesLength)
                .HasColumnName("notes");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => e.LeagueId);
            builder.HasIndex(e => e.TeamId);
            builder.HasIndex(e => e.Year);
            builder.HasIndex(e => new { e.LeagueId, e.IdentityKey })
                .IsUnique()
                .HasFilter("deleted_at IS NULL")
                .HasDatabaseName("UX_league_champions_identity");

            builder.HasOne(e => e.League)
                .WithMany()
                .HasForeignKey(e => e.LeagueId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Team)
                .WithMany()
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Season)
                .WithMany()
                .HasForeignKey(e => e.SeasonId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(e => e.Division)
                .WithMany()
                .HasForeignKey(e => e.DivisionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(e => e.Photos)
                .WithOne(p => p.LeagueChampion)
                .HasForeignKey(p => p.LeagueChampionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(e => e.Photos).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
