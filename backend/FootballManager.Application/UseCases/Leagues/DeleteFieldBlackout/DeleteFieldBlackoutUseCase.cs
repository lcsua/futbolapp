using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.DeleteFieldBlackout
{
    public class DeleteFieldBlackoutUseCase : IDeleteFieldBlackoutUseCase
    {
        private readonly IFieldRepository _fieldRepository;
        private readonly IFieldBlackoutRepository _blackoutRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteFieldBlackoutUseCase(
            IFieldRepository fieldRepository,
            IFieldBlackoutRepository blackoutRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
            _blackoutRepository = blackoutRepository ?? throw new ArgumentNullException(nameof(blackoutRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(DeleteFieldBlackoutRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var field = await _fieldRepository.GetByIdAsync(request.FieldId, cancellationToken);
            if (field == null || field.LeagueId != request.LeagueId)
                throw new KeyNotFoundException($"Field {request.FieldId} not found or does not belong to league.");

            var blackout = await _blackoutRepository.GetByIdAsync(request.BlackoutId, cancellationToken);
            if (blackout == null || blackout.FieldId != request.FieldId)
                throw new KeyNotFoundException($"Blackout {request.BlackoutId} not found.");

            _blackoutRepository.Remove(blackout);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
