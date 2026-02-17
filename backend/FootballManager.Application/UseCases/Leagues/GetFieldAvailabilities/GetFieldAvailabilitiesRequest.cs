using System;

namespace FootballManager.Application.UseCases.Leagues.GetFieldAvailabilities
{
    public class GetFieldAvailabilitiesRequest
    {
        public Guid LeagueId { get; }
        public Guid FieldId { get; }
        public Guid UserId { get; }

        public GetFieldAvailabilitiesRequest(Guid leagueId, Guid fieldId, Guid userId)
        {
            LeagueId = leagueId;
            FieldId = fieldId;
            UserId = userId;
        }
    }
}
