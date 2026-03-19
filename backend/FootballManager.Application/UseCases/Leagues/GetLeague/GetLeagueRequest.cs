using System;

namespace FootballManager.Application.UseCases.Leagues.GetLeague
{
    public class GetLeagueRequest
    {
        public Guid LeagueId { get; }
        public Guid UserId { get; }
        public bool IsPublic { get; }

        public GetLeagueRequest(Guid leagueId, Guid userId, bool isPublic = false)
        {
            LeagueId = leagueId;
            UserId = userId;
            IsPublic = isPublic;
        }
    }
}
