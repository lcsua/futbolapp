using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Api.Services;
using FootballManager.Application.UseCases.Leagues.AdvertisementImages;
using FootballManager.Application.UseCases.Leagues.LeagueChampions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers
{
    [ApiController]
    [Route("api/leagues/{leagueId}/champions")]
    public class LeagueChampionsController : ControllerBase
    {
        private readonly IGetLeagueChampionsUseCase _getLeagueChampionsUseCase;
        private readonly IGetLeagueChampionUseCase _getLeagueChampionUseCase;
        private readonly IGetLeagueChampionOptionsUseCase _getLeagueChampionOptionsUseCase;
        private readonly ICreateLeagueChampionUseCase _createLeagueChampionUseCase;
        private readonly IUpdateLeagueChampionUseCase _updateLeagueChampionUseCase;
        private readonly IDeleteLeagueChampionUseCase _deleteLeagueChampionUseCase;
        private readonly IAddLeagueChampionPhotosUseCase _addLeagueChampionPhotosUseCase;
        private readonly IUpdateLeagueChampionGalleryUseCase _updateLeagueChampionGalleryUseCase;
        private readonly IDeleteLeagueChampionPhotoUseCase _deleteLeagueChampionPhotoUseCase;

        public LeagueChampionsController(
            IGetLeagueChampionsUseCase getLeagueChampionsUseCase,
            IGetLeagueChampionUseCase getLeagueChampionUseCase,
            IGetLeagueChampionOptionsUseCase getLeagueChampionOptionsUseCase,
            ICreateLeagueChampionUseCase createLeagueChampionUseCase,
            IUpdateLeagueChampionUseCase updateLeagueChampionUseCase,
            IDeleteLeagueChampionUseCase deleteLeagueChampionUseCase,
            IAddLeagueChampionPhotosUseCase addLeagueChampionPhotosUseCase,
            IUpdateLeagueChampionGalleryUseCase updateLeagueChampionGalleryUseCase,
            IDeleteLeagueChampionPhotoUseCase deleteLeagueChampionPhotoUseCase)
        {
            _getLeagueChampionsUseCase = getLeagueChampionsUseCase;
            _getLeagueChampionUseCase = getLeagueChampionUseCase;
            _getLeagueChampionOptionsUseCase = getLeagueChampionOptionsUseCase;
            _createLeagueChampionUseCase = createLeagueChampionUseCase;
            _updateLeagueChampionUseCase = updateLeagueChampionUseCase;
            _deleteLeagueChampionUseCase = deleteLeagueChampionUseCase;
            _addLeagueChampionPhotosUseCase = addLeagueChampionPhotosUseCase;
            _updateLeagueChampionGalleryUseCase = updateLeagueChampionGalleryUseCase;
            _deleteLeagueChampionPhotoUseCase = deleteLeagueChampionPhotoUseCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetChampions(
            [FromRoute] Guid leagueId,
            [FromQuery] int? year,
            [FromQuery] string? championship,
            [FromQuery] string? division,
            [FromQuery] Guid? teamId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _getLeagueChampionsUseCase.ExecuteAsync(leagueId, userId, new LeagueChampionListFilter
            {
                Year = year,
                Championship = championship,
                Division = division,
                TeamId = teamId,
            }, cancellationToken);

            return Ok(response);
        }

        [HttpGet("options")]
        public async Task<IActionResult> GetOptions([FromRoute] Guid leagueId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _getLeagueChampionOptionsUseCase.ExecuteAsync(leagueId, userId, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{championId:guid}")]
        public async Task<IActionResult> GetChampion(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid championId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _getLeagueChampionUseCase.ExecuteAsync(leagueId, championId, userId, cancellationToken);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> CreateChampion(
            [FromRoute] Guid leagueId,
            [FromBody] ChampionWriteBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _createLeagueChampionUseCase.ExecuteAsync(
                ToRequest(leagueId, userId, body),
                cancellationToken);

            return CreatedAtAction(nameof(GetChampion), new { leagueId, championId = response.Id }, response);
        }

        [HttpPut("{championId:guid}")]
        public async Task<IActionResult> UpdateChampion(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid championId,
            [FromBody] ChampionWriteBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _updateLeagueChampionUseCase.ExecuteAsync(
                championId,
                ToRequest(leagueId, userId, body),
                cancellationToken);

            return Ok(response);
        }

        [HttpDelete("{championId:guid}")]
        public async Task<IActionResult> DeleteChampion(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid championId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var urls = await _deleteLeagueChampionUseCase.ExecuteAsync(leagueId, championId, userId, cancellationToken);
            foreach (var url in urls)
                ChampionPhotoStorage.TryDeleteWithThumb(url);

            return NoContent();
        }

        [HttpPost("{championId:guid}/photos")]
        [RequestSizeLimit(40 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPhotos(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid championId,
            [FromForm] UploadChampionPhotosRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var files = request?.Files?.Where(f => f != null).ToList() ?? new List<IFormFile>();
            if (files.Count == 0)
                return BadRequest(new { message = "Seleccioná al menos una imagen." });
            if (files.Count > 12)
                return BadRequest(new { message = "Se pueden subir hasta 12 imágenes por vez." });

            foreach (var file in files)
            {
                var validationError = AdvertisementImageRules.Validate(file.FileName, file.ContentType, file.Length);
                if (validationError != null)
                    return BadRequest(new { message = validationError });
            }

            var savedUrls = new List<string>();
            try
            {
                foreach (var file in files)
                {
                    var url = await ChampionPhotoStorage.SaveAsync(leagueId, championId, file, cancellationToken);
                    savedUrls.Add(url);
                }

                var response = await _addLeagueChampionPhotosUseCase.ExecuteAsync(
                    leagueId,
                    championId,
                    userId,
                    savedUrls,
                    cancellationToken);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                foreach (var url in savedUrls)
                    ChampionPhotoStorage.TryDeleteWithThumb(url);
                return BadRequest(new { message = ex.Message });
            }
            catch
            {
                foreach (var url in savedUrls)
                    ChampionPhotoStorage.TryDeleteWithThumb(url);
                throw;
            }
        }

        [HttpPut("{championId:guid}/photos")]
        public async Task<IActionResult> UpdateGallery(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid championId,
            [FromBody] ChampionGalleryBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var photos = (body?.Photos ?? new List<ChampionPhotoBody>())
                .Select(p => new LeagueChampionPhotoWrite
                {
                    Id = p.Id,
                    Caption = p.Caption,
                    DisplayOrder = p.DisplayOrder,
                    IsCover = p.IsCover,
                })
                .ToList();

            var response = await _updateLeagueChampionGalleryUseCase.ExecuteAsync(
                leagueId,
                championId,
                userId,
                photos,
                cancellationToken);
            return Ok(response);
        }

        [HttpDelete("{championId:guid}/photos/{photoId:guid}")]
        public async Task<IActionResult> DeletePhoto(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid championId,
            [FromRoute] Guid photoId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var imageUrl = await _deleteLeagueChampionPhotoUseCase.ExecuteAsync(
                leagueId,
                championId,
                photoId,
                userId,
                cancellationToken);
            ChampionPhotoStorage.TryDeleteWithThumb(imageUrl);
            return NoContent();
        }

        private static LeagueChampionWriteRequest ToRequest(Guid leagueId, Guid userId, ChampionWriteBody? body)
        {
            return new LeagueChampionWriteRequest
            {
                LeagueId = leagueId,
                UserId = userId,
                Mode = body?.Mode ?? string.Empty,
                TeamId = body?.TeamId ?? Guid.Empty,
                SeasonId = body?.SeasonId,
                DivisionId = body?.DivisionId,
                Year = body?.Year,
                ChampionshipName = body?.ChampionshipName,
                DivisionName = body?.DivisionName,
                WonAt = body?.WonAt,
                Notes = body?.Notes,
            };
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Guid.Empty;
            return userId;
        }
    }

    public class ChampionWriteBody
    {
        public string Mode { get; set; } = string.Empty;
        public Guid TeamId { get; set; }
        public Guid? SeasonId { get; set; }
        public Guid? DivisionId { get; set; }
        public int? Year { get; set; }
        public string? ChampionshipName { get; set; }
        public string? DivisionName { get; set; }
        public DateOnly? WonAt { get; set; }
        public string? Notes { get; set; }
    }

    public class ChampionGalleryBody
    {
        public List<ChampionPhotoBody> Photos { get; set; } = new();
    }

    public class ChampionPhotoBody
    {
        public Guid Id { get; set; }
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsCover { get; set; }
    }

    public class UploadChampionPhotosRequest
    {
        public List<IFormFile>? Files { get; set; }
    }
}
