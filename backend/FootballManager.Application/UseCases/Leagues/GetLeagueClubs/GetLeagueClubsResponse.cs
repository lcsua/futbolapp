using System.Collections.Generic;
using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueClubs
{
    public class GetLeagueClubsResponse
    {
        public List<ClubDto> Clubs { get; }

        public GetLeagueClubsResponse(List<ClubDto> clubs)
        {
            Clubs = clubs ?? new List<ClubDto>();
        }
    }
}
