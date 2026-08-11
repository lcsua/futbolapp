using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Leagues.CreateDocument;
using FootballManager.Application.UseCases.Leagues.CreateDocumentCategory;
using FootballManager.Application.UseCases.Leagues.DeleteDocument;
using FootballManager.Application.UseCases.Leagues.DeleteDocumentCategory;
using FootballManager.Application.UseCases.Leagues.GetDocumentCategories;
using FootballManager.Application.UseCases.Leagues.GetDocuments;
using FootballManager.Application.UseCases.Leagues.SeedLeagueDocumentDefaults;
using FootballManager.Application.UseCases.Leagues.UpdateDocument;
using FootballManager.Application.UseCases.Leagues.UpdateDocumentCategory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers
{
    [ApiController]
    [Route("api/leagues/{leagueId}")]
    public class LeagueDocumentsController : ControllerBase
    {
        private readonly IGetDocumentCategoriesUseCase _getDocumentCategoriesUseCase;
        private readonly ICreateDocumentCategoryUseCase _createDocumentCategoryUseCase;
        private readonly IUpdateDocumentCategoryUseCase _updateDocumentCategoryUseCase;
        private readonly IDeleteDocumentCategoryUseCase _deleteDocumentCategoryUseCase;
        private readonly IGetDocumentsUseCase _getDocumentsUseCase;
        private readonly ICreateDocumentUseCase _createDocumentUseCase;
        private readonly IUpdateDocumentUseCase _updateDocumentUseCase;
        private readonly IDeleteDocumentUseCase _deleteDocumentUseCase;
        private readonly ISeedLeagueDocumentDefaultsUseCase _seedLeagueDocumentDefaultsUseCase;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public LeagueDocumentsController(
            IGetDocumentCategoriesUseCase getDocumentCategoriesUseCase,
            ICreateDocumentCategoryUseCase createDocumentCategoryUseCase,
            IUpdateDocumentCategoryUseCase updateDocumentCategoryUseCase,
            IDeleteDocumentCategoryUseCase deleteDocumentCategoryUseCase,
            IGetDocumentsUseCase getDocumentsUseCase,
            ICreateDocumentUseCase createDocumentUseCase,
            IUpdateDocumentUseCase updateDocumentUseCase,
            IDeleteDocumentUseCase deleteDocumentUseCase,
            ISeedLeagueDocumentDefaultsUseCase seedLeagueDocumentDefaultsUseCase,
            IUserLeagueRepository userLeagueRepository)
        {
            _getDocumentCategoriesUseCase = getDocumentCategoriesUseCase;
            _createDocumentCategoryUseCase = createDocumentCategoryUseCase;
            _updateDocumentCategoryUseCase = updateDocumentCategoryUseCase;
            _deleteDocumentCategoryUseCase = deleteDocumentCategoryUseCase;
            _getDocumentsUseCase = getDocumentsUseCase;
            _createDocumentUseCase = createDocumentUseCase;
            _updateDocumentUseCase = updateDocumentUseCase;
            _deleteDocumentUseCase = deleteDocumentUseCase;
            _seedLeagueDocumentDefaultsUseCase = seedLeagueDocumentDefaultsUseCase;
            _userLeagueRepository = userLeagueRepository;
        }

        [HttpGet("document-categories")]
        public async Task<IActionResult> GetDocumentCategories([FromRoute] Guid leagueId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _getDocumentCategoriesUseCase.ExecuteAsync(new GetDocumentCategoriesRequest
            {
                LeagueId = leagueId,
                UserId = userId,
            }, cancellationToken);

            return Ok(response.Categories);
        }

        [HttpPost("document-categories")]
        public async Task<IActionResult> CreateDocumentCategory(
            [FromRoute] Guid leagueId,
            [FromBody] CreateDocumentCategoryBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _createDocumentCategoryUseCase.ExecuteAsync(new CreateDocumentCategoryRequest
            {
                LeagueId = leagueId,
                UserId = userId,
                Name = body?.Name ?? string.Empty,
                RequiresDocumentDate = body?.RequiresDocumentDate ?? false,
                SortOrder = body?.SortOrder,
            }, cancellationToken);

            return CreatedAtAction(nameof(GetDocumentCategories), new { leagueId }, response);
        }

        [HttpPut("document-categories/{id}")]
        public async Task<IActionResult> UpdateDocumentCategory(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid id,
            [FromBody] UpdateDocumentCategoryBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _updateDocumentCategoryUseCase.ExecuteAsync(new UpdateDocumentCategoryRequest
            {
                LeagueId = leagueId,
                CategoryId = id,
                UserId = userId,
                Name = body?.Name ?? string.Empty,
                RequiresDocumentDate = body?.RequiresDocumentDate ?? false,
                SortOrder = body?.SortOrder ?? 0,
                IsActive = body?.IsActive ?? true,
                Slug = body?.Slug,
            }, cancellationToken);

            return NoContent();
        }

        [HttpDelete("document-categories/{id}")]
        public async Task<IActionResult> DeleteDocumentCategory(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _deleteDocumentCategoryUseCase.ExecuteAsync(new DeleteDocumentCategoryRequest
            {
                LeagueId = leagueId,
                CategoryId = id,
                UserId = userId,
            }, cancellationToken);

            return NoContent();
        }

        [HttpPost("document-categories/seed-defaults")]
        public async Task<IActionResult> SeedDocumentCategoryDefaults(
            [FromRoute] Guid leagueId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _seedLeagueDocumentDefaultsUseCase.ExecuteAsync(new SeedLeagueDocumentDefaultsRequest
            {
                LeagueId = leagueId,
                UserId = userId,
                RequireMembership = true,
            }, cancellationToken);

            return Ok(response);
        }

        [HttpGet("documents")]
        public async Task<IActionResult> GetDocuments(
            [FromRoute] Guid leagueId,
            [FromQuery] Guid? categoryId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _getDocumentsUseCase.ExecuteAsync(new GetDocumentsRequest
            {
                LeagueId = leagueId,
                UserId = userId,
                CategoryId = categoryId,
            }, cancellationToken);

            return Ok(response.Documents);
        }

        [HttpPost("documents")]
        public async Task<IActionResult> CreateDocument(
            [FromRoute] Guid leagueId,
            [FromBody] CreateDocumentBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _createDocumentUseCase.ExecuteAsync(new CreateDocumentRequest
            {
                LeagueId = leagueId,
                UserId = userId,
                CategoryId = body?.CategoryId ?? Guid.Empty,
                Title = body?.Title ?? string.Empty,
                Description = body?.Description,
                DocumentDate = body?.DocumentDate,
                FileUrl = body?.FileUrl ?? string.Empty,
                RelativePath = body?.RelativePath ?? string.Empty,
                ContentType = body?.ContentType ?? string.Empty,
                FileSizeBytes = body?.FileSizeBytes ?? 0,
                OriginalFileName = body?.OriginalFileName ?? string.Empty,
                SortOrder = body?.SortOrder,
                IsPublished = body?.IsPublished ?? true,
            }, cancellationToken);

            return CreatedAtAction(nameof(GetDocuments), new { leagueId }, response);
        }

        [HttpPut("documents/{id}")]
        public async Task<IActionResult> UpdateDocument(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid id,
            [FromBody] UpdateDocumentBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _updateDocumentUseCase.ExecuteAsync(new UpdateDocumentRequest
            {
                LeagueId = leagueId,
                DocumentId = id,
                UserId = userId,
                Title = body?.Title ?? string.Empty,
                Description = body?.Description,
                DocumentDate = body?.DocumentDate,
                SortOrder = body?.SortOrder,
                IsPublished = body?.IsPublished ?? true,
                FileUrl = body?.FileUrl,
                RelativePath = body?.RelativePath,
                ContentType = body?.ContentType,
                FileSizeBytes = body?.FileSizeBytes,
                OriginalFileName = body?.OriginalFileName,
            }, cancellationToken);

            return NoContent();
        }

        [HttpDelete("documents/{id}")]
        public async Task<IActionResult> DeleteDocument(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _deleteDocumentUseCase.ExecuteAsync(new DeleteDocumentRequest
            {
                LeagueId = leagueId,
                DocumentId = id,
                UserId = userId,
            }, cancellationToken);

            TryDeleteUploadedFile(response.RelativePath);
            return NoContent();
        }

        [HttpPost("uploads/documents")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument(
            [FromRoute] Guid leagueId,
            [FromForm] UploadLeagueDocumentRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(userId, leagueId, cancellationToken);
            if (!hasAccess) return Forbid();

            var file = request?.File;
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "A document file is required." });

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var imageExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var allowedExts = new HashSet<string>(imageExts) { ".pdf" };

            if (string.IsNullOrWhiteSpace(ext) || !allowedExts.Contains(ext))
                return BadRequest(new { message = "Allowed extensions: .pdf, .jpg, .jpeg, .png, .webp, .gif" });

            var isPdf = ext == ".pdf";
            var maxBytes = isPdf ? 10 * 1024 * 1024L : 5 * 1024 * 1024L;
            if (file.Length > maxBytes)
                return BadRequest(new { message = isPdf ? "PDF size must be up to 10 MB." : "Image size must be up to 5 MB." });

            if (isPdf)
            {
                if (string.IsNullOrWhiteSpace(file.ContentType) ||
                    !file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "PDF files must have content type application/pdf." });
            }
            else if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only image or PDF uploads are allowed." });
            }

            var relativeDir = Path.Combine("uploads", "leagues", leagueId.ToString(), "documents");
            var rootDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativeDir);
            Directory.CreateDirectory(rootDir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(rootDir, fileName);

            await using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(fs, cancellationToken);
            }

            var relativeUrl = $"/{relativeDir.Replace("\\", "/")}/{fileName}";
            var publicUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";

            return Ok(new
            {
                url = publicUrl,
                relativeUrl,
                contentType = file.ContentType,
                fileSizeBytes = file.Length,
                originalFileName = Path.GetFileName(file.FileName),
            });
        }

        private static void TryDeleteUploadedFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            try
            {
                var trimmed = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", trimmed);
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            catch
            {
                // Best-effort file cleanup; DB row already removed.
            }
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Guid.Empty;
            return userId;
        }
    }

    public class CreateDocumentCategoryBody
    {
        public string Name { get; set; } = string.Empty;
        public bool RequiresDocumentDate { get; set; }
        public int? SortOrder { get; set; }
    }

    public class UpdateDocumentCategoryBody
    {
        public string Name { get; set; } = string.Empty;
        public bool RequiresDocumentDate { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Slug { get; set; }
    }

    public class CreateDocumentBody
    {
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

    public class UpdateDocumentBody
    {
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

    public class UploadLeagueDocumentRequest
    {
        public IFormFile? File { get; set; }
    }
}
