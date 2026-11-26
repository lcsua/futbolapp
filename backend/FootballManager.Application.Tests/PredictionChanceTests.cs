using FootballManager.Application.Predictions;

namespace FootballManager.Application.Tests;

public class PredictionChanceTests
{
    [Fact]
    public void Format_uses_zero_and_hundred_for_certainties()
    {
        Assert.Equal("0%", PredictionChance.Format(0));
        Assert.Equal("100%", PredictionChance.Format(1));
        Assert.Equal("<0,1%", PredictionChance.Format(0.0005));
        Assert.Equal("75,5%", PredictionChance.Format(0.755));
    }

    [Fact]
    public void Favorite_bands_follow_editorial_thresholds()
    {
        Assert.Equal("Partido parejo", MatchFavoriteClassifier.Classify(0.40, 0.30, 0.30, "A", "B").ToneLabel);
        Assert.Equal("Ligero favorito", MatchFavoriteClassifier.Classify(0.48, 0.27, 0.25, "A", "B").ToneLabel);
        Assert.Equal("Favorito", MatchFavoriteClassifier.Classify(0.60, 0.22, 0.18, "A", "B").ToneLabel);
        Assert.Equal("Claro favorito", MatchFavoriteClassifier.Classify(0.75, 0.15, 0.10, "A", "B").ToneLabel);
        Assert.Equal("Amplio favorito", MatchFavoriteClassifier.Classify(0.88, 0.08, 0.04, "A", "B").ToneLabel);
        Assert.Equal("Ninguno", MatchFavoriteClassifier.Classify(0.30, 0.40, 0.30, "A", "B").FavoriteLabel);
        Assert.Equal("Encuentro decisivo", MatchFavoriteClassifier.Classify(0.40, 0.30, 0.30, "A", "B", 0.20, 0.18).ToneLabel);
    }

    [Fact]
    public void No_change_only_when_rounded_pp_is_zero()
    {
        Assert.True(PredictionChance.IsNoChange(0.0004));
        Assert.False(PredictionChance.IsNoChange(0.01));
        Assert.Equal("+1,0 pp", PredictionChance.FormatPoints(1.0));
    }
}
