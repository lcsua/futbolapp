using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueSeasons
{
    public class GetLeagueSeasonsRequest
    {
        public Guid LeagueId { get; }
        public Guid UserId { get; }

        public GetLeagueSeasonsRequest(Guid leagueId, Guid userId)
        {
            LeagueId = leagueId;
            UserId = userId;
        }
    }

    public class GetLeagueSeasonsResponse
    {
        public List<SeasonDto> Seasons { get; }

        public GetLeagueSeasonsResponse(List<SeasonDto> seasons)
        {
            Seasons = seasons;
        }
    }

    public interface IGetLeagueSeasonsUseCase
    {
        Task<GetLeagueSeasonsResponse> ExecuteAsync(GetLeagueSeasonsRequest request, CancellationToken cancellationToken = default);
    }

    public class GetLeagueSeasonsUseCase : IGetLeagueSeasonsUseCase
    {
        private readonly ISeasonRepository _seasonRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetLeagueSeasonsUseCase(ISeasonRepository seasonRepository, IUserLeagueRepository userLeagueRepository)
        {
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetLeagueSeasonsResponse> ExecuteAsync(GetLeagueSeasonsRequest request, CancellationToken cancellationToken = default)
        {
            // Authorization Check: Only members can view seasons
            if (!await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken))
            {
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");
            }

            var seasons = await _seasonRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
            var seasonDtos = seasons.ConvertAll(s => new SeasonDto(s.Id, s.Name, s.StartDate, s.EndDate));
            return new GetLeagueSeasonsResponse(seasonDtos);
        }
    }
}
