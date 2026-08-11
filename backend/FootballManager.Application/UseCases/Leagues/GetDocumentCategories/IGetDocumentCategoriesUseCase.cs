using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.GetDocumentCategories
{
    public interface IGetDocumentCategoriesUseCase
    {
        Task<GetDocumentCategoriesResponse> ExecuteAsync(GetDocumentCategoriesRequest request, CancellationToken cancellationToken = default);
    }
}
