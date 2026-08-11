using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.UpdateDocumentCategory
{
    public interface IUpdateDocumentCategoryUseCase
    {
        Task ExecuteAsync(UpdateDocumentCategoryRequest request, CancellationToken cancellationToken = default);
    }

    public class UpdateDocumentCategoryRequest
    {
        public Guid LeagueId { get; set; }
        public Guid CategoryId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool RequiresDocumentDate { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Slug { get; set; }
    }
}
