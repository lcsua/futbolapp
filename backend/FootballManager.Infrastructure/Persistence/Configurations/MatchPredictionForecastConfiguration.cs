using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations;

public class MatchPredictionForecastConfiguration : IEntityTypeConfiguration<MatchPredictionForecast>
{
    public void Configure(EntityTypeBuilder<MatchPredictionForecast> builder)
    {
        builder.ToTable("match_prediction_forecasts");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.LeagueSlug).HasMaxLength(120).IsRequired();
        builder.Property(item => item.SeasonSlug).HasMaxLength(120).IsRequired();
        builder.Property(item => item.DivisionSlug).HasMaxLength(120).IsRequired();
        builder.Property(item => item.ModelVersion).HasMaxLength(40).IsRequired();
        builder.HasIndex(item => new { item.MatchId, item.ModelVersion }).IsUnique();
        builder.HasIndex(item => new { item.LeagueSlug, item.SeasonSlug, item.DivisionSlug, item.Round });
    }
}
