using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.UpsertCompetitionRule
{
    public class UpsertCompetitionRuleUseCase : IUpsertCompetitionRuleUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly ICompetitionRuleRepository _ruleRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpsertCompetitionRuleUseCase(
            ILeagueRepository leagueRepository,
            ICompetitionRuleRepository ruleRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UpsertCompetitionRuleRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            // Regla global por liga: ignoramos seasonId aunque llegue desde cliente antiguo.
            var rule = await _ruleRepository.GetByLeagueAndSeasonAsync(request.LeagueId, null, cancellationToken);
            if (rule == null)
            {
                rule = new CompetitionRule(league, request.MatchesPerWeek, request.IsHomeAway, season: null);
                await _ruleRepository.AddAsync(rule, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                rule.UpdateDetails(request.MatchesPerWeek, request.IsHomeAway);
                _ruleRepository.Update(rule);
            }

            await _ruleRepository.RemoveMatchDaysAsync(rule.Id, cancellationToken);
            foreach (var day in request.MatchDays ?? new List<int>())
            {
                if (day < 0 || day > 6) continue;
                await _ruleRepository.AddMatchDayAsync(new CompetitionMatchDay(rule, day), cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
