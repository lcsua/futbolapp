using System;

namespace FootballManager.Application.UseCases.Leagues.GetLeague
{
    public class GetLeagueRequest
    {
        public Guid LeagueId { get; }
        public Guid UserId { get; }

        public GetLeagueRequest(Guid leagueId, Guid userId)
        {
            LeagueId = leagueId;
            UserId = userId;
        }
    }
}
