using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GetLeague
{
    public class GetLeagueResponse
    {
        public LeagueDto League { get; }

        public GetLeagueResponse(LeagueDto league)
        {
            League = league;
        }
    }
}
