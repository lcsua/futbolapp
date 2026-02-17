using System.Collections.Generic;
using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueFields
{
    public class GetLeagueFieldsResponse
    {
        public List<FieldDto> Fields { get; }

        public GetLeagueFieldsResponse(List<FieldDto> fields)
        {
            Fields = fields;
        }
    }
}
