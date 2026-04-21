using System;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueClubs
{
    public class GetLeagueClubsRequest
    {
        public Guid LeagueId { get; }
        public Guid UserId { get; }

        public GetLeagueClubsRequest(Guid leagueId, Guid userId)
        {
            LeagueId = leagueId;
            UserId = userId;
        }
    }
}
