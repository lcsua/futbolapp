using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.CreateField
{
    public class CreateFieldUseCase : ICreateFieldUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly IFieldRepository _fieldRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateFieldUseCase(
            ILeagueRepository leagueRepository,
            IFieldRepository fieldRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<CreateFieldResponse> ExecuteAsync(CreateFieldRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Field name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            var field = new Field(league, request.Name);
            field.SetLocation(request.Address, request.City, request.GeoLat, request.GeoLng);
            field.SetDescription(request.Description);
            field.SetAvailability(request.IsAvailable);
            await _fieldRepository.AddAsync(field, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateFieldResponse(field.Id);
        }
    }
}
