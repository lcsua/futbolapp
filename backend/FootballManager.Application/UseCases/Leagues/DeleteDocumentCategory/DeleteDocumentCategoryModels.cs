using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.DeleteDocumentCategory
{
    public interface IDeleteDocumentCategoryUseCase
    {
        Task ExecuteAsync(DeleteDocumentCategoryRequest request, CancellationToken cancellationToken = default);
    }

    public class DeleteDocumentCategoryRequest
    {
        public Guid LeagueId { get; set; }
        public Guid CategoryId { get; set; }
        public Guid UserId { get; set; }
    }
}
