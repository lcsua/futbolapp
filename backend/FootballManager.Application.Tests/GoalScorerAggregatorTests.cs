using FootballManager.Application.Helpers;

namespace FootballManager.Application.Tests;

public class GoalScorerAggregatorTests
{
    [Fact]
    public void Aggregate_SumsGoalsByPlayerId()
    {
        var id = Guid.NewGuid();
        var rows = GoalScorerAggregator.Aggregate(new (Guid?, string)[]
        {
            (id, "Juan"),
            (id, "Juan"),
            (Guid.NewGuid(), "Pedro")
        });

        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].Goals);
        Assert.Equal("Juan", rows[0].PlayerName);
        Assert.Equal(1, rows[1].Goals);
    }

    [Fact]
    public void Aggregate_GroupsNamelessGoalsByNormalizedName()
    {
        var rows = GoalScorerAggregator.Aggregate(new (Guid?, string)[]
        {
            (null, "lópez"),
            (null, "López"),
            (null, "  "),
            (null, "")
        });

        Assert.Single(rows);
        Assert.Equal(2, rows[0].Goals);
        Assert.Equal("lópez", rows[0].PlayerName, ignoreCase: true);
    }

    [Fact]
    public void ResolveDisplayName_PrefersNicknameThenFullName()
    {
        Assert.Equal("El Tano", GoalScorerAggregator.ResolveDisplayName("Juan Perez", "El Tano", "Juan", "Perez"));
        Assert.Equal("Juan Perez", GoalScorerAggregator.ResolveDisplayName("JP", null, "Juan", "Perez"));
        Assert.Equal("JP", GoalScorerAggregator.ResolveDisplayName("JP", null, null, null));
    }
}
