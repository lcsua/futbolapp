using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.UpsertMatchRule
{
    public interface IUpsertMatchRuleUseCase
    {
        Task ExecuteAsync(UpsertMatchRuleRequest request, CancellationToken cancellationToken = default);
    }
}
