using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetFieldAvailabilities
{
    public class GetFieldAvailabilitiesUseCase : IGetFieldAvailabilitiesUseCase
    {
        private readonly IFieldRepository _fieldRepository;
        private readonly IFieldAvailabilityRepository _availabilityRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetFieldAvailabilitiesUseCase(
            IFieldRepository fieldRepository,
            IFieldAvailabilityRepository availabilityRepository,
            IUserLeagueRepository userLeagueRepository)
        {
            _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
            _availabilityRepository = availabilityRepository ?? throw new ArgumentNullException(nameof(availabilityRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetFieldAvailabilitiesResponse> ExecuteAsync(GetFieldAvailabilitiesRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var field = await _fieldRepository.GetByIdAsync(request.FieldId, cancellationToken);
            if (field == null || field.LeagueId != request.LeagueId)
                throw new KeyNotFoundException($"Field {request.FieldId} not found or does not belong to league.");

            var list = await _availabilityRepository.GetByFieldIdAsync(request.FieldId, cancellationToken);
            var items = list.Select(a => new FieldAvailabilityItem(a.Id, a.DayOfWeek, a.StartTime, a.EndTime, a.IsActive)).ToList();
            return new GetFieldAvailabilitiesResponse(items);
        }
    }
}
