using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GenerateSeasonFixtures;

public sealed class GenerateSeasonFixturesResponse
{
    public FixtureDraftDto Draft { get; }

    public GenerateSeasonFixturesResponse(FixtureDraftDto draft)
    {
        Draft = draft;
    }
}
