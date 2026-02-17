using System;

namespace FootballManager.Application.UseCases.Leagues.GetFieldBlackouts
{
    public class GetFieldBlackoutsRequest
    {
        public Guid LeagueId { get; }
        public Guid FieldId { get; }
        public Guid UserId { get; }

        public GetFieldBlackoutsRequest(Guid leagueId, Guid fieldId, Guid userId)
        {
            LeagueId = leagueId;
            FieldId = fieldId;
            UserId = userId;
        }
    }
}
