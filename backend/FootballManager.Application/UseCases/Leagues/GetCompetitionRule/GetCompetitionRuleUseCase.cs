using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetCompetitionRule
{
    public class GetCompetitionRuleUseCase : IGetCompetitionRuleUseCase
    {
        private readonly ICompetitionRuleRepository _ruleRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetCompetitionRuleUseCase(ICompetitionRuleRepository ruleRepository, IUserLeagueRepository userLeagueRepository)
        {
            _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetCompetitionRuleResponse?> ExecuteAsync(GetCompetitionRuleRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var rule = await _ruleRepository.GetByLeagueAndSeasonAsync(request.LeagueId, request.SeasonId, cancellationToken);
            if (rule == null)
                return null;

            var matchDays = rule.MatchDays.OrderBy(m => m.DayOfWeek).Select(m => m.DayOfWeek).ToList();
            return new GetCompetitionRuleResponse(rule.Id, rule.LeagueId, rule.SeasonId, rule.MatchesPerWeek, rule.IsHomeAway, matchDays);
        }
    }
}
