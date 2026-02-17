using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetFieldBlackouts
{
    public class GetFieldBlackoutsUseCase : IGetFieldBlackoutsUseCase
    {
        private readonly IFieldRepository _fieldRepository;
        private readonly IFieldBlackoutRepository _blackoutRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetFieldBlackoutsUseCase(
            IFieldRepository fieldRepository,
            IFieldBlackoutRepository blackoutRepository,
            IUserLeagueRepository userLeagueRepository)
        {
            _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
            _blackoutRepository = blackoutRepository ?? throw new ArgumentNullException(nameof(blackoutRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetFieldBlackoutsResponse> ExecuteAsync(GetFieldBlackoutsRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var field = await _fieldRepository.GetByIdAsync(request.FieldId, cancellationToken);
            if (field == null || field.LeagueId != request.LeagueId)
                throw new KeyNotFoundException($"Field {request.FieldId} not found or does not belong to league.");

            var list = await _blackoutRepository.GetByFieldIdAsync(request.FieldId, cancellationToken);
            var items = list.Select(b => new FieldBlackoutItem(b.Id, b.Date, b.StartTime, b.EndTime, b.Reason)).ToList();
            return new GetFieldBlackoutsResponse(items);
        }
    }
}
