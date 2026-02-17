using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.GetMatchRule
{
    public interface IGetMatchRuleUseCase
    {
        Task<GetMatchRuleResponse?> ExecuteAsync(GetMatchRuleRequest request, CancellationToken cancellationToken = default);
    }
}
