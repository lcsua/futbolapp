using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.GetDocumentCategories
{
    public class GetDocumentCategoriesRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
    }

    public class GetDocumentCategoriesResponse
    {
        public List<DocumentCategoryDto> Categories { get; }

        public GetDocumentCategoriesResponse(List<DocumentCategoryDto> categories)
        {
            Categories = categories ?? new List<DocumentCategoryDto>();
        }
    }

    public record DocumentCategoryDto(
        Guid Id,
        string Name,
        string Slug,
        int SortOrder,
        bool RequiresDocumentDate,
        bool IsActive);
}
