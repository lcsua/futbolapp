using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.CreateDocument
{
    public interface ICreateDocumentUseCase
    {
        Task<CreateDocumentResponse> ExecuteAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default);
    }

    public class CreateDocumentRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
        public Guid CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly? DocumentDate { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public int? SortOrder { get; set; }
        public bool IsPublished { get; set; } = true;
    }

    public class CreateDocumentResponse
    {
        public Guid Id { get; }

        public CreateDocumentResponse(Guid id)
        {
            Id = id;
        }
    }
}
