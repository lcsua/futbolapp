namespace FootballManager.Application.Predictions;

/// <summary>
/// Editorial favorite bands for a three-way result.
/// Draw as the unique mode never makes a side the favorite.
/// </summary>
public static class MatchFavoriteClassifier
{
    public const double EvenCeiling = 0.45;
    public const double SlightCeiling = 0.55;
    public const double FavoriteCeiling = 0.70;
    public const double ClearCeiling = 0.85;
    public const double DecisiveTitleFloor = 0.05;

    public static (string ToneLabel, string FavoriteLabel) Classify(
        double homeWin,
        double draw,
        double awayWin,
        string homeName,
        string awayName,
        double homeTitleProbability = 0,
        double awayTitleProbability = 0)
    {
        if (homeTitleProbability >= DecisiveTitleFloor && awayTitleProbability >= DecisiveTitleFloor)
            return ("Encuentro decisivo", FavoriteSide(homeWin, draw, awayWin, homeName, awayName));

        var sideTop = Math.Max(homeWin, awayWin);
        var drawLeads = draw >= homeWin && draw >= awayWin;
        if (drawLeads || sideTop < EvenCeiling)
            return ("Partido parejo", "Ninguno");

        var favorite = homeWin >= awayWin ? homeName : awayName;
        var tone = sideTop < SlightCeiling
            ? "Ligero favorito"
            : sideTop < FavoriteCeiling
                ? "Favorito"
                : sideTop < ClearCeiling
                    ? "Claro favorito"
                    : "Amplio favorito";
        return (tone, favorite);
    }

    private static string FavoriteSide(double homeWin, double draw, double awayWin, string homeName, string awayName)
    {
        if (draw >= homeWin && draw >= awayWin)
            return "Ninguno";
        if (homeWin >= awayWin && homeWin > draw)
            return homeName;
        if (awayWin > homeWin && awayWin > draw)
            return awayName;
        return "Ninguno";
    }
}
