using System.Text.Json;
using FootballManager.Infrastructure.ProfessionalFootball;

namespace FootballManager.Application.Tests;

public class EspnScoreboardParserTests
{
    [Fact]
    public void Parse_ExtractsMatchFields()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "espn-scoreboard-sample.json"));
        using var doc = JsonDocument.Parse(json);
        var matches = EspnScoreboardParser.Parse(doc.RootElement);

        Assert.Single(matches);
        var m = matches[0];
        Assert.Equal("1001", m.ExternalId);
        Assert.Equal("pre", m.Status);
        Assert.Equal("River Plate", m.HomeTeam.Name);
        Assert.Equal("Independiente", m.AwayTeam.Name);
        Assert.Equal("Estadio Monumental", m.Venue);
        Assert.Equal(new DateTimeOffset(2026, 8, 8, 23, 30, 0, TimeSpan.Zero), m.Date);
        Assert.Equal(0, m.HomeScore);
        Assert.Equal(0, m.AwayScore);
    }
}
