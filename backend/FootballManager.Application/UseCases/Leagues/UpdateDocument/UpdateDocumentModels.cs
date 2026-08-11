using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.UpdateDocument
{
    public interface IUpdateDocumentUseCase
    {
        Task ExecuteAsync(UpdateDocumentRequest request, CancellationToken cancellationToken = default);
    }

    public class UpdateDocumentRequest
    {
        public Guid LeagueId { get; set; }
        public Guid DocumentId { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly? DocumentDate { get; set; }
        public int? SortOrder { get; set; }
        public bool IsPublished { get; set; } = true;
        public string? FileUrl { get; set; }
        public string? RelativePath { get; set; }
        public string? ContentType { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? OriginalFileName { get; set; }
    }
}
