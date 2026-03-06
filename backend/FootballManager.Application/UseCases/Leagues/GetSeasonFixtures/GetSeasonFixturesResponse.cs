using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GetSeasonFixtures;

public sealed class GetSeasonFixturesResponse
{
    public FixtureDraftDto Fixtures { get; }
    public bool IsDraft { get; }

    public GetSeasonFixturesResponse(FixtureDraftDto fixtures, bool isDraft)
    {
        Fixtures = fixtures;
        IsDraft = isDraft;
    }
}
