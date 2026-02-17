using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.GetCompetitionRule
{
    public interface IGetCompetitionRuleUseCase
    {
        Task<GetCompetitionRuleResponse?> ExecuteAsync(GetCompetitionRuleRequest request, CancellationToken cancellationToken = default);
    }
}
