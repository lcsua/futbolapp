using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.GetDocuments
{
    public interface IGetDocumentsUseCase
    {
        Task<GetDocumentsResponse> ExecuteAsync(GetDocumentsRequest request, CancellationToken cancellationToken = default);
    }

    public class GetDocumentsRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
        public Guid? CategoryId { get; set; }
    }

    public class GetDocumentsResponse
    {
        public List<DocumentDto> Documents { get; }

        public GetDocumentsResponse(List<DocumentDto> documents)
        {
            Documents = documents ?? new List<DocumentDto>();
        }
    }

    public record DocumentDto(
        Guid Id,
        Guid CategoryId,
        string Title,
        string? Description,
        string FileUrl,
        string RelativePath,
        string OriginalFileName,
        string ContentType,
        long FileSizeBytes,
        DateOnly? DocumentDate,
        int SortOrder,
        bool IsPublished);
}
