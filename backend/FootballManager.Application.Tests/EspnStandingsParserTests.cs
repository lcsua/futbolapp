using System.Text.Json;
using FootballManager.Infrastructure.ProfessionalFootball;

namespace FootballManager.Application.Tests;

public class EspnStandingsParserTests
{
    [Fact]
    public void Parse_ExtractsGroupsTeamsAndStats()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "espn-standings-sample.json"));
        using var doc = JsonDocument.Parse(json);
        var groups = EspnStandingsParser.Parse(doc.RootElement);

        Assert.Equal(2, groups.Count);
        Assert.Equal("Grupo A", groups[0].Name);
        Assert.Equal("Grupo B", groups[1].Name);
        Assert.Equal(2, groups[0].Entries.Count);

        var boca = groups[0].Entries[0];
        Assert.Equal(1, boca.Position);
        Assert.Equal("Boca Juniors", boca.TeamName);
        Assert.Equal("5", boca.TeamExternalId);
        Assert.Equal(7, boca.Played);
        Assert.Equal(4, boca.Won);
        Assert.Equal(2, boca.Drawn);
        Assert.Equal(1, boca.Lost);
        Assert.Equal(10, boca.GoalsFor);
        Assert.Equal(5, boca.GoalsAgainst);
        Assert.Equal(5, boca.GoalDifference);
        Assert.Equal(14, boca.Points);
        Assert.Contains("5.png", boca.TeamLogo);

        Assert.Equal("River Plate", groups[1].Entries[0].TeamName);
        Assert.Equal(16, groups[1].Entries[0].Points);
    }
}
