using FootballManager.Application.UseCases.Matches.ImportMatchResults;

namespace FootballManager.Application.Tests;

public class ImportMatchResultsCreateGuardTests
{
    [Fact]
    public void Does_not_create_pairs_when_the_csv_round_already_has_fixtures()
    {
        Assert.False(ImportMatchResultsUseCase.CanCreateMissingPairs(csvRound: 2, fixturesInTargetRound: 10));
    }

    [Fact]
    public void Creates_pairs_when_the_csv_round_has_no_fixtures_yet()
    {
        Assert.True(ImportMatchResultsUseCase.CanCreateMissingPairs(csvRound: 3, fixturesInTargetRound: 0));
    }

    [Fact]
    public void Creates_pairs_when_round_is_not_specified()
    {
        Assert.True(ImportMatchResultsUseCase.CanCreateMissingPairs(csvRound: null, fixturesInTargetRound: 10));
    }
}
