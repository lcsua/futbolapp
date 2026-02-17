using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class FixtureConfiguration : IEntityTypeConfiguration<Fixture>
    {
        public void Configure(EntityTypeBuilder<Fixture> builder)
        {
            builder.ToTable("fixtures", t => t.HasCheckConstraint("CK_fixtures_home_away_different", "home_team_division_season_id != away_team_division_season_id"));

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.LeagueId).HasColumnName("league_id");
            builder.Property(e => e.SeasonId).HasColumnName("season_id");
            builder.Property(e => e.DivisionSeasonId).HasColumnName("division_season_id");
            builder.Property(e => e.HomeTeamDivisionSeasonId).HasColumnName("home_team_division_season_id");
            builder.Property(e => e.AwayTeamDivisionSeasonId).HasColumnName("away_team_division_season_id");
            builder.Property(e => e.RoundNumber).HasColumnName("round_number");
            builder.Property(e => e.MatchDate).HasColumnName("match_date");
            builder.Property(e => e.StartTime).HasColumnName("start_time");
            builder.Property(e => e.FieldId).HasColumnName("field_id");
            builder.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status")
                .HasConversion<string>();
            builder.Property(e => e.RefereeName).HasMaxLength(100).HasColumnName("referee_name");
            builder.Property(e => e.Attendance).HasColumnName("attendance");
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasIndex(e => new { e.LeagueId, e.SeasonId });
            builder.HasIndex(e => new { e.FieldId, e.MatchDate });
            builder.HasIndex(e => new { e.DivisionSeasonId, e.RoundNumber });

            builder.HasOne(e => e.League)
                .WithMany()
                .HasForeignKey(e => e.LeagueId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.Season)
                .WithMany()
                .HasForeignKey(e => e.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.DivisionSeason)
                .WithMany(ds => ds.Fixtures)
                .HasForeignKey(e => e.DivisionSeasonId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.HomeTeamDivisionSeason)
                .WithMany()
                .HasForeignKey(e => e.HomeTeamDivisionSeasonId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.AwayTeamDivisionSeason)
                .WithMany()
                .HasForeignKey(e => e.AwayTeamDivisionSeasonId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.Field)
                .WithMany()
                .HasForeignKey(e => e.FieldId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.Result)
                .WithOne(e => e.Fixture)
                .HasForeignKey<Result>(e => e.FixtureId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
