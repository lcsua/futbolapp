using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.UpsertCompetitionRule
{
    public interface IUpsertCompetitionRuleUseCase
    {
        Task ExecuteAsync(UpsertCompetitionRuleRequest request, CancellationToken cancellationToken = default);
    }
}
