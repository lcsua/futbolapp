using System.Globalization;

namespace FootballManager.Application.Predictions;

/// <summary>
/// Shared chance labels for title, podium and match probabilities.
/// Matemáticamente imposible → 0%. Asegurado → 100%.
/// Probabilidad positiva menor a 0,1% → &lt;0,1%.
/// </summary>
public static class PredictionChance
{
    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    public static string Format(double probability)
    {
        if (double.IsNaN(probability) || probability <= 0)
            return "0%";
        if (probability >= 1)
            return "100%";
        var points = probability * 100;
        if (points < 0.1)
            return "<0,1%";
        if (points > 99.9)
            return ">99,9%";
        return points.ToString("0.0", EsAr) + "%";
    }

    public static string FormatPoints(double probabilityPoints)
    {
        var text = Math.Abs(probabilityPoints) < 0.05
            ? "0,0"
            : probabilityPoints.ToString("+0.0;-0.0", EsAr);
        return text + " pp";
    }

    public static bool IsNoChange(double deltaProbability) =>
        Math.Abs(deltaProbability * 100) < 0.05;
}
