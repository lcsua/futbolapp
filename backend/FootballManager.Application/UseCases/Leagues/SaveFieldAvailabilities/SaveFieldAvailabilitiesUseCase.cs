using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.SaveFieldAvailabilities
{
    public class SaveFieldAvailabilitiesUseCase : ISaveFieldAvailabilitiesUseCase
    {
        private readonly IFieldRepository _fieldRepository;
        private readonly IFieldAvailabilityRepository _availabilityRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SaveFieldAvailabilitiesUseCase(
            IFieldRepository fieldRepository,
            IFieldAvailabilityRepository availabilityRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
            _availabilityRepository = availabilityRepository ?? throw new ArgumentNullException(nameof(availabilityRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(SaveFieldAvailabilitiesRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var field = await _fieldRepository.GetByIdAsync(request.FieldId, cancellationToken);
            if (field == null || field.LeagueId != request.LeagueId)
                throw new KeyNotFoundException($"Field {request.FieldId} not found or does not belong to league.");

            await _availabilityRepository.RemoveAllByFieldIdAsync(request.FieldId, cancellationToken);
            foreach (var slot in request.Slots ?? new List<FieldAvailabilitySlot>())
            {
                if (slot.DayOfWeek < 0 || slot.DayOfWeek > 6) continue;
                var availability = new FieldAvailability(field, slot.DayOfWeek, slot.StartTime, slot.EndTime);
                if (!slot.IsActive)
                    availability.SetActive(false);
                await _availabilityRepository.AddAsync(availability, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
