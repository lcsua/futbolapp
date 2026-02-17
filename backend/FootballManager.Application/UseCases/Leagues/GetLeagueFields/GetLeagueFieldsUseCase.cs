using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueFields
{
    public class GetLeagueFieldsUseCase : IGetLeagueFieldsUseCase
    {
        private readonly IFieldRepository _fieldRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetLeagueFieldsUseCase(
            IFieldRepository fieldRepository,
            IUserLeagueRepository userLeagueRepository)
        {
            _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetLeagueFieldsResponse> ExecuteAsync(GetLeagueFieldsRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var fields = await _fieldRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
            var dtos = fields.Select(f => new FieldDto(f.Id, f.Name, f.Address, f.City, f.GeoLat, f.GeoLng, f.IsAvailable, f.Description)).ToList();
            return new GetLeagueFieldsResponse(dtos);
        }
    }
}
