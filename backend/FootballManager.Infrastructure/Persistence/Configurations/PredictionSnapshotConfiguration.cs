using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballManager.Infrastructure.Persistence.Configurations;

public class PredictionSnapshotConfiguration : IEntityTypeConfiguration<PredictionSnapshot>
{
    public void Configure(EntityTypeBuilder<PredictionSnapshot> builder)
    {
        builder.ToTable("prediction_snapshots");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.LeagueSlug).HasColumnName("league_slug").HasMaxLength(160).IsRequired();
        builder.Property(item => item.SeasonSlug).HasColumnName("season_slug").HasMaxLength(160).IsRequired();
        builder.Property(item => item.DivisionSlug).HasColumnName("division_slug").HasMaxLength(160).IsRequired();
        builder.Property(item => item.TeamId).HasColumnName("team_id");
        builder.Property(item => item.CutoffRound).HasColumnName("cutoff_round");
        builder.Property(item => item.Reconstructed).HasColumnName("reconstructed");
        builder.Property(item => item.TitleProbability).HasColumnName("title_probability");
        builder.Property(item => item.PodiumProbability).HasColumnName("podium_probability");
        builder.Property(item => item.ExpectedPosition).HasColumnName("expected_position");
        builder.Property(item => item.ExpectedPoints).HasColumnName("expected_points");
        builder.Property(item => item.ModelVersion).HasColumnName("model_version").HasMaxLength(40).IsRequired();
        builder.Property(item => item.CalculatedAt).HasColumnName("calculated_at");
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at");
        builder.Property(item => item.DeletedAt).HasColumnName("deleted_at");
        builder.HasIndex(item => new
        {
            item.LeagueSlug,
            item.SeasonSlug,
            item.DivisionSlug,
            item.TeamId,
            item.CutoffRound,
            item.ModelVersion,
            item.Reconstructed
        }).IsUnique().HasDatabaseName("ux_prediction_snapshots_cutoff");
    }
}
