using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.CreateDocumentCategory
{
    public interface ICreateDocumentCategoryUseCase
    {
        Task<CreateDocumentCategoryResponse> ExecuteAsync(CreateDocumentCategoryRequest request, CancellationToken cancellationToken = default);
    }

    public class CreateDocumentCategoryRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool RequiresDocumentDate { get; set; }
        public int? SortOrder { get; set; }
    }

    public class CreateDocumentCategoryResponse
    {
        public Guid Id { get; }
        public string Slug { get; }

        public CreateDocumentCategoryResponse(Guid id, string slug)
        {
            Id = id;
            Slug = slug;
        }
    }
}
