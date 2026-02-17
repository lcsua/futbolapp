using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.UpsertMatchRule
{
    public class UpsertMatchRuleUseCase : IUpsertMatchRuleUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IMatchRuleRepository _ruleRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpsertMatchRuleUseCase(
            ILeagueRepository leagueRepository,
            ISeasonRepository seasonRepository,
            IMatchRuleRepository ruleRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UpsertMatchRuleRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            Season season = null;
            if (request.SeasonId.HasValue)
            {
                season = await _seasonRepository.GetByIdAsync(request.SeasonId.Value, cancellationToken);
                if (season == null || season.LeagueId != request.LeagueId)
                    throw new KeyNotFoundException($"Season {request.SeasonId} not found or does not belong to league.");
            }

            var rule = await _ruleRepository.GetByLeagueAndSeasonAsync(request.LeagueId, request.SeasonId, cancellationToken);
            if (rule == null)
            {
                rule = new MatchRule(league, request.HalfMinutes, request.BreakMinutes, request.WarmupBufferMinutes, request.SlotGranularityMinutes, request.FirstMatchToleranceMinutes, season);
                await _ruleRepository.AddAsync(rule, cancellationToken);
            }
            else
            {
                rule.UpdateDetails(request.HalfMinutes, request.BreakMinutes, request.WarmupBufferMinutes, request.SlotGranularityMinutes, request.FirstMatchToleranceMinutes);
                _ruleRepository.Update(rule);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
