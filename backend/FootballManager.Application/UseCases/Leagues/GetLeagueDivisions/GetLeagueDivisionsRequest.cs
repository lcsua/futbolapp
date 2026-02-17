using System;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueDivisions
{
    public class GetLeagueDivisionsRequest
    {
        public Guid LeagueId { get; }
        public Guid UserId { get; }

        public GetLeagueDivisionsRequest(Guid leagueId, Guid userId)
        {
            LeagueId = leagueId;
            UserId = userId;
        }
    }
}
