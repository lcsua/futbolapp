using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations
{
    public class ResultConfiguration : IEntityTypeConfiguration<Result>
    {
        public void Configure(EntityTypeBuilder<Result> builder)
        {
            builder.ToTable("results");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");

            builder.Property(e => e.FixtureId).HasColumnName("fixture_id");

            builder.Property(e => e.HomeTeamGoals).IsRequired().HasDefaultValue(0).HasColumnName("home_team_goals");
            builder.Property(e => e.AwayTeamGoals).IsRequired().HasDefaultValue(0).HasColumnName("away_team_goals");
            builder.Property(e => e.Notes).HasColumnName("notes");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            // Relationship handled in FixtureConfiguration due to cyclic ref, but redundancy is fine or just relying on FixtureConfig.
            // But best practice is to configure in one place or be consistent.
            // The HasForeignKey<Result>(e => e.FixtureId) is in FixtureConfig.
        }
    }
}
