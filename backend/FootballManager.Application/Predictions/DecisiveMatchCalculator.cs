namespace FootballManager.Application.Predictions;

public sealed record DecisiveMatchReport(
    Guid MatchId,
    int Round,
    Guid HomeTeamId,
    Guid AwayTeamId,
    string HomeName,
    string AwayName,
    string ImpactLevel,
    string Reason,
    double MaxTitleSwing,
    double HomeSwing,
    double AwaySwing);

/// <summary>
/// Picks the next-round matches with the largest title-probability swing using the
/// already-cached what-if samples. Impact is the max absolute change in title
/// chance for either side across win/draw/loss versus the live baseline.
/// </summary>
public static class DecisiveMatchCalculator
{
    public static IReadOnlyList<DecisiveMatchReport> Build(
        IReadOnlyList<MatchOutlook> nextRound,
        IReadOnlyList<TeamOutlook> teams,
        IReadOnlyList<WhatIfReport>? whatIfs,
        int take = 3)
    {
        if (nextRound.Count == 0 || whatIfs == null || whatIfs.Count == 0)
            return Array.Empty<DecisiveMatchReport>();

        var byTeam = teams.ToDictionary(team => team.TeamId);
        var byWhatIf = whatIfs.ToDictionary(item => item.TeamId);
        var reports = new List<DecisiveMatchReport>();

        foreach (var match in nextRound)
        {
            byTeam.TryGetValue(match.HomeTeamId, out var home);
            byTeam.TryGetValue(match.AwayTeamId, out var away);
            byWhatIf.TryGetValue(match.HomeTeamId, out var homeIf);
            byWhatIf.TryGetValue(match.AwayTeamId, out var awayIf);

            var homeSwing = Swing(home?.TitleProbability ?? 0, homeIf);
            var awaySwing = Swing(away?.TitleProbability ?? 0, awayIf);
            var maxSwing = Math.Max(homeSwing, awaySwing);
            if (maxSwing < 0.005)
                continue;

            var impact = maxSwing >= 0.08 ? "alto" : maxSwing >= 0.03 ? "medio" : "bajo";
            var reason = Reason(home, away, homeSwing, awaySwing, maxSwing);
            reports.Add(new DecisiveMatchReport(
                match.MatchId,
                match.Round,
                match.HomeTeamId,
                match.AwayTeamId,
                match.HomeName,
                match.AwayName,
                impact,
                reason,
                maxSwing,
                homeSwing,
                awaySwing));
        }

        return reports
            .OrderByDescending(item => item.MaxTitleSwing)
            .ThenByDescending(item => Math.Max(
                byTeam.GetValueOrDefault(item.HomeTeamId)?.TitleProbability ?? 0,
                byTeam.GetValueOrDefault(item.AwayTeamId)?.TitleProbability ?? 0))
            .Take(take)
            .ToList();
    }

    private static double Swing(double current, WhatIfReport? report)
    {
        if (report == null)
            return 0;
        var deltas = new List<double>();
        if (report.WinReliable) deltas.Add(Math.Abs(report.WinTitle - current));
        if (report.DrawReliable) deltas.Add(Math.Abs(report.DrawTitle - current));
        if (report.LossReliable) deltas.Add(Math.Abs(report.LossTitle - current));
        return deltas.Count == 0 ? 0 : deltas.Max();
    }

    private static string Reason(
        TeamOutlook? home,
        TeamOutlook? away,
        double homeSwing,
        double awaySwing,
        double maxSwing)
    {
        var focus = homeSwing >= awaySwing ? home : away;
        var other = ReferenceEquals(focus, home) ? away : home;
        if (focus == null)
            return "El resultado puede mover las chances de título.";
        if (other != null && focus.TitleProbability >= 0.05 && other.TitleProbability >= 0.05)
            return $"Cruce entre candidatos: {focus.Name} y {other.Name} todavía pelean el título.";
        if (maxSwing >= 0.08)
            return $"El resultado puede mover más de {Math.Round(maxSwing * 100):0} puntos de chance de título para {focus.Name}.";
        return $"El resultado cambia con claridad las chances de título de {focus.Name}.";
    }
}
