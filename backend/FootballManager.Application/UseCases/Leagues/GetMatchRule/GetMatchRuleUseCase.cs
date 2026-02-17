using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetMatchRule
{
    public class GetMatchRuleUseCase : IGetMatchRuleUseCase
    {
        private readonly IMatchRuleRepository _ruleRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetMatchRuleUseCase(IMatchRuleRepository ruleRepository, IUserLeagueRepository userLeagueRepository)
        {
            _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetMatchRuleResponse?> ExecuteAsync(GetMatchRuleRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var rule = await _ruleRepository.GetByLeagueAndSeasonAsync(request.LeagueId, request.SeasonId, cancellationToken);
            if (rule == null)
                return null;

            return new GetMatchRuleResponse(rule.Id, rule.LeagueId, rule.SeasonId, rule.HalfMinutes, rule.BreakMinutes, rule.WarmupBufferMinutes, rule.SlotGranularityMinutes, rule.FirstMatchToleranceMinutes);
        }
    }
}
