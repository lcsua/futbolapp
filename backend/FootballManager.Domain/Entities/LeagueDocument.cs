using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class LeagueDocument : Entity
    {
        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; } = null!;

        public Guid CategoryId { get; private set; }
        public virtual LeagueDocumentCategory Category { get; private set; } = null!;

        public string Title { get; private set; } = string.Empty;
        public string? Description { get; private set; }

        public string FileUrl { get; private set; } = string.Empty;
        public string RelativePath { get; private set; } = string.Empty;
        public string OriginalFileName { get; private set; } = string.Empty;
        public string ContentType { get; private set; } = string.Empty;
        public long FileSizeBytes { get; private set; }

        public DateOnly? DocumentDate { get; private set; }
        public int SortOrder { get; private set; }
        public bool IsPublished { get; private set; }

        protected LeagueDocument() { }

        public LeagueDocument(
            League league,
            LeagueDocumentCategory category,
            string title,
            string fileUrl,
            string relativePath,
            string originalFileName,
            string contentType,
            long fileSizeBytes,
            string? description = null,
            DateOnly? documentDate = null,
            int sortOrder = 0,
            bool isPublished = true)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            Category = category ?? throw new ArgumentNullException(nameof(category));
            CategoryId = category.Id;
            Title = !string.IsNullOrWhiteSpace(title) ? title.Trim() : throw new ArgumentException("Document title cannot be empty.", nameof(title));
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            FileUrl = !string.IsNullOrWhiteSpace(fileUrl) ? fileUrl.Trim() : throw new ArgumentException("File URL cannot be empty.", nameof(fileUrl));
            RelativePath = !string.IsNullOrWhiteSpace(relativePath) ? relativePath.Trim() : throw new ArgumentException("Relative path cannot be empty.", nameof(relativePath));
            OriginalFileName = !string.IsNullOrWhiteSpace(originalFileName) ? originalFileName.Trim() : throw new ArgumentException("Original file name cannot be empty.", nameof(originalFileName));
            ContentType = !string.IsNullOrWhiteSpace(contentType) ? contentType.Trim() : throw new ArgumentException("Content type cannot be empty.", nameof(contentType));
            FileSizeBytes = fileSizeBytes >= 0 ? fileSizeBytes : throw new ArgumentOutOfRangeException(nameof(fileSizeBytes));
            DocumentDate = documentDate;
            SortOrder = sortOrder;
            IsPublished = isPublished;
        }

        public void Update(
            string title,
            string? description,
            DateOnly? documentDate,
            bool isPublished,
            string? fileUrl = null,
            string? relativePath = null,
            string? originalFileName = null,
            string? contentType = null,
            long? fileSizeBytes = null)
        {
            Title = !string.IsNullOrWhiteSpace(title) ? title.Trim() : throw new ArgumentException("Document title cannot be empty.", nameof(title));
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            DocumentDate = documentDate;
            IsPublished = isPublished;

            if (!string.IsNullOrWhiteSpace(fileUrl))
                FileUrl = fileUrl.Trim();
            if (!string.IsNullOrWhiteSpace(relativePath))
                RelativePath = relativePath.Trim();
            if (!string.IsNullOrWhiteSpace(originalFileName))
                OriginalFileName = originalFileName.Trim();
            if (!string.IsNullOrWhiteSpace(contentType))
                ContentType = contentType.Trim();
            if (fileSizeBytes.HasValue)
            {
                if (fileSizeBytes.Value < 0)
                    throw new ArgumentOutOfRangeException(nameof(fileSizeBytes));
                FileSizeBytes = fileSizeBytes.Value;
            }

            UpdateTimestamp();
        }

        public void SetSortOrder(int sortOrder)
        {
            SortOrder = sortOrder;
            UpdateTimestamp();
        }
    }
}
