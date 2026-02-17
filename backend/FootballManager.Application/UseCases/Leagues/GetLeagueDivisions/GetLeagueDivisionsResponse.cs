using System.Collections.Generic;
using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueDivisions
{
    public class GetLeagueDivisionsResponse
    {
        public List<DivisionDto> Divisions { get; }

        public GetLeagueDivisionsResponse(List<DivisionDto> divisions)
        {
            Divisions = divisions;
        }
    }
}
