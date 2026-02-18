using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Leagues.GetSeasonSetup;
using FootballManager.Application.UseCases.Leagues.SaveSeasonSetup;

namespace FootballManager.Application.UseCases.Leagues.CopySeasonFrom
{
    public class CopySeasonFromUseCase : ICopySeasonFromUseCase
    {
        private readonly IGetSeasonSetupUseCase _getSeasonSetupUseCase;
        private readonly ISaveSeasonSetupUseCase _saveSeasonSetupUseCase;

        public CopySeasonFromUseCase(
            IGetSeasonSetupUseCase getSeasonSetupUseCase,
            ISaveSeasonSetupUseCase saveSeasonSetupUseCase)
        {
            _getSeasonSetupUseCase = getSeasonSetupUseCase ?? throw new ArgumentNullException(nameof(getSeasonSetupUseCase));
            _saveSeasonSetupUseCase = saveSeasonSetupUseCase ?? throw new ArgumentNullException(nameof(saveSeasonSetupUseCase));
        }

        public async Task ExecuteAsync(CopySeasonFromRequest request, CancellationToken cancellationToken = default)
        {
            if (request.SeasonId == request.SourceSeasonId)
                throw new BusinessException("Target and source season must be different.");

            var getRequest = new GetSeasonSetupRequest(request.LeagueId, request.SourceSeasonId, request.UserId);
            var sourceSetup = await _getSeasonSetupUseCase.ExecuteAsync(getRequest, cancellationToken);

            var divisionsToSave = sourceSetup.Divisions
                .Select(d => new SaveSeasonSetupDivisionDto
                {
                    DivisionId = d.DivisionId,
                    TeamIds = d.Teams.Select(t => t.Id).ToList()
                })
                .ToList();

            var saveRequest = new SaveSeasonSetupRequest
            {
                LeagueId = request.LeagueId,
                SeasonId = request.SeasonId,
                UserId = request.UserId,
                Divisions = divisionsToSave
            };
            await _saveSeasonSetupUseCase.ExecuteAsync(saveRequest, cancellationToken);
        }
    }
}
