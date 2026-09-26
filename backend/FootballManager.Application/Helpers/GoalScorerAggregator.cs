namespace FootballManager.Application.Helpers;

public static class GoalScorerAggregator
{
    public readonly record struct ScorerRow(Guid? PlayerId, string PlayerName, int Goals);

    public static IReadOnlyList<ScorerRow> Aggregate(IEnumerable<(Guid? PlayerId, string PlayerName)> goals)
    {
        return goals
            .Select(g =>
            {
                var name = (g.PlayerName ?? string.Empty).Trim();
                return (g.PlayerId, Name: name);
            })
            .Where(g => g.PlayerId.HasValue || g.Name.Length > 0)
            .GroupBy(g => g.PlayerId.HasValue
                ? "id:" + g.PlayerId.Value
                : "name:" + g.Name.ToUpperInvariant())
            .Select(g =>
            {
                var firstWithName = g.FirstOrDefault(x => x.Name.Length > 0);
                var name = firstWithName.Name.Length > 0 ? firstWithName.Name : "Jugador";
                return new ScorerRow(g.First().PlayerId, name, g.Count());
            })
            .OrderByDescending(s => s.Goals)
            .ThenBy(s => s.PlayerName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string ResolveDisplayName(string? storedName, string? nickname, string? firstName, string? lastName)
    {
        if (!string.IsNullOrWhiteSpace(nickname))
            return nickname.Trim();
        var full = $"{firstName} {lastName}".Trim();
        if (full.Length > 0)
            return full;
        return (storedName ?? string.Empty).Trim();
    }
}
