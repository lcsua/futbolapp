using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.UpdateField
{
    public class UpdateFieldUseCase : IUpdateFieldUseCase
    {
        private readonly IFieldRepository _fieldRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateFieldUseCase(
            IFieldRepository fieldRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UpdateFieldRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Field name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var field = await _fieldRepository.GetByIdAsync(request.FieldId, cancellationToken);
            if (field == null)
                throw new KeyNotFoundException($"Field {request.FieldId} not found.");
            if (field.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Field does not belong to this league.");

            field.UpdateDetails(request.Name, request.Address, request.City, request.GeoLat, request.GeoLng, request.IsAvailable, request.Description);
            _fieldRepository.Update(field);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
