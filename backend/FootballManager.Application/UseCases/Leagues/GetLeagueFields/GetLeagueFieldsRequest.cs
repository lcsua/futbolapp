using System;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueFields
{
    public class GetLeagueFieldsRequest
    {
        public Guid LeagueId { get; }
        public Guid UserId { get; }

        public GetLeagueFieldsRequest(Guid leagueId, Guid userId)
        {
            LeagueId = leagueId;
            UserId = userId;
        }
    }
}
