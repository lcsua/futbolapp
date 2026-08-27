using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Api.Services;
using FootballManager.Application.UseCases.Leagues.AdvertisementImages;
using FootballManager.Application.UseCases.Leagues.CreateAdvertisement;
using FootballManager.Application.UseCases.Leagues.DeleteAdvertisement;
using FootballManager.Application.UseCases.Leagues.GetAdvertisement;
using FootballManager.Application.UseCases.Leagues.GetAdvertisements;
using FootballManager.Application.UseCases.Leagues.RemoveAdvertisementImage;
using FootballManager.Application.UseCases.Leagues.SetAdvertisementImage;
using FootballManager.Application.UseCases.Leagues.UpdateAdvertisement;
using FootballManager.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers
{
    [ApiController]
    [Route("api/leagues/{leagueId}/advertisements")]
    public class LeagueAdvertisementsController : ControllerBase
    {
        private readonly IGetAdvertisementsUseCase _getAdvertisementsUseCase;
        private readonly IGetAdvertisementUseCase _getAdvertisementUseCase;
        private readonly ICreateAdvertisementUseCase _createAdvertisementUseCase;
        private readonly IUpdateAdvertisementUseCase _updateAdvertisementUseCase;
        private readonly IDeleteAdvertisementUseCase _deleteAdvertisementUseCase;
        private readonly ISetAdvertisementImageUseCase _setAdvertisementImageUseCase;
        private readonly IRemoveAdvertisementImageUseCase _removeAdvertisementImageUseCase;

        public LeagueAdvertisementsController(
            IGetAdvertisementsUseCase getAdvertisementsUseCase,
            IGetAdvertisementUseCase getAdvertisementUseCase,
            ICreateAdvertisementUseCase createAdvertisementUseCase,
            IUpdateAdvertisementUseCase updateAdvertisementUseCase,
            IDeleteAdvertisementUseCase deleteAdvertisementUseCase,
            ISetAdvertisementImageUseCase setAdvertisementImageUseCase,
            IRemoveAdvertisementImageUseCase removeAdvertisementImageUseCase)
        {
            _getAdvertisementsUseCase = getAdvertisementsUseCase;
            _getAdvertisementUseCase = getAdvertisementUseCase;
            _createAdvertisementUseCase = createAdvertisementUseCase;
            _updateAdvertisementUseCase = updateAdvertisementUseCase;
            _deleteAdvertisementUseCase = deleteAdvertisementUseCase;
            _setAdvertisementImageUseCase = setAdvertisementImageUseCase;
            _removeAdvertisementImageUseCase = removeAdvertisementImageUseCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetAdvertisements([FromRoute] Guid leagueId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _getAdvertisementsUseCase.ExecuteAsync(new GetAdvertisementsRequest
            {
                LeagueId = leagueId,
                UserId = userId,
            }, cancellationToken);

            return Ok(response.Advertisements);
        }

        [HttpGet("{advertisementId}")]
        public async Task<IActionResult> GetAdvertisement(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid advertisementId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var advertisement = await _getAdvertisementUseCase.ExecuteAsync(new GetAdvertisementRequest
            {
                LeagueId = leagueId,
                AdvertisementId = advertisementId,
                UserId = userId,
            }, cancellationToken);

            return Ok(advertisement);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdvertisement(
            [FromRoute] Guid leagueId,
            [FromBody] AdvertisementBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _createAdvertisementUseCase.ExecuteAsync(new CreateAdvertisementRequest
            {
                LeagueId = leagueId,
                UserId = userId,
                Name = body?.Name ?? string.Empty,
                AdvertiserName = body?.AdvertiserName ?? string.Empty,
                TargetUrl = body?.TargetUrl,
                Slot = body?.Slot ?? default,
                StartsAt = body?.StartsAt,
                EndsAt = body?.EndsAt,
                Priority = body?.Priority ?? 0,
                IsActive = body?.IsActive ?? true,
            }, cancellationToken);

            return CreatedAtAction(
                nameof(GetAdvertisement),
                new { leagueId, advertisementId = response.Id },
                response);
        }

        [HttpPut("{advertisementId}")]
        public async Task<IActionResult> UpdateAdvertisement(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid advertisementId,
            [FromBody] AdvertisementBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _updateAdvertisementUseCase.ExecuteAsync(new UpdateAdvertisementRequest
            {
                LeagueId = leagueId,
                AdvertisementId = advertisementId,
                UserId = userId,
                Name = body?.Name ?? string.Empty,
                AdvertiserName = body?.AdvertiserName ?? string.Empty,
                TargetUrl = body?.TargetUrl,
                Slot = body?.Slot ?? default,
                StartsAt = body?.StartsAt,
                EndsAt = body?.EndsAt,
                Priority = body?.Priority ?? 0,
                IsActive = body?.IsActive ?? true,
            }, cancellationToken);

            return NoContent();
        }

        [HttpDelete("{advertisementId}")]
        public async Task<IActionResult> DeleteAdvertisement(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid advertisementId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _deleteAdvertisementUseCase.ExecuteAsync(new DeleteAdvertisementRequest
            {
                LeagueId = leagueId,
                AdvertisementId = advertisementId,
                UserId = userId,
            }, cancellationToken);

            AdvertisementImageStorage.TryDeleteManaged(response.DesktopImageUrl);
            AdvertisementImageStorage.TryDeleteManaged(response.MobileImageUrl);
            return NoContent();
        }

        [HttpPost("{advertisementId}/desktop-image")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public Task<IActionResult> UploadDesktopImage(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid advertisementId,
            [FromForm] UploadAdvertisementImageRequest request,
            CancellationToken cancellationToken)
            => UploadImageAsync(leagueId, advertisementId, AdvertisementImageKind.Desktop, request, cancellationToken);

        [HttpPost("{advertisementId}/mobile-image")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public Task<IActionResult> UploadMobileImage(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid advertisementId,
            [FromForm] UploadAdvertisementImageRequest request,
            CancellationToken cancellationToken)
            => UploadImageAsync(leagueId, advertisementId, AdvertisementImageKind.Mobile, request, cancellationToken);

        [HttpDelete("{advertisementId}/desktop-image")]
        public Task<IActionResult> DeleteDesktopImage(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid advertisementId,
            CancellationToken cancellationToken)
            => DeleteImageAsync(leagueId, advertisementId, AdvertisementImageKind.Desktop, cancellationToken);

        [HttpDelete("{advertisementId}/mobile-image")]
        public Task<IActionResult> DeleteMobileImage(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid advertisementId,
            CancellationToken cancellationToken)
            => DeleteImageAsync(leagueId, advertisementId, AdvertisementImageKind.Mobile, cancellationToken);

        private async Task<IActionResult> UploadImageAsync(
            Guid leagueId,
            Guid advertisementId,
            AdvertisementImageKind kind,
            UploadAdvertisementImageRequest? request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var file = request?.File;
            var validationError = AdvertisementImageRules.Validate(file?.FileName, file?.ContentType, file?.Length ?? 0);
            if (validationError != null)
                return BadRequest(new { message = validationError });

            string? savedPath = null;
            try
            {
                var (publicUrl, fullPath) = await AdvertisementImageStorage.SaveAsync(
                    Request, leagueId, advertisementId, kind, file!, cancellationToken);
                savedPath = fullPath;

                var response = await _setAdvertisementImageUseCase.ExecuteAsync(new SetAdvertisementImageRequest
                {
                    LeagueId = leagueId,
                    AdvertisementId = advertisementId,
                    UserId = userId,
                    Kind = kind,
                    ImageUrl = publicUrl,
                }, cancellationToken);

                if (!string.Equals(response.PreviousImageUrl, publicUrl, StringComparison.OrdinalIgnoreCase))
                    AdvertisementImageStorage.TryDeleteManaged(response.PreviousImageUrl);

                return Ok(response.Advertisement);
            }
            catch
            {
                AdvertisementImageStorage.TryDeleteFile(savedPath);
                throw;
            }
        }

        private async Task<IActionResult> DeleteImageAsync(
            Guid leagueId,
            Guid advertisementId,
            AdvertisementImageKind kind,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _removeAdvertisementImageUseCase.ExecuteAsync(new RemoveAdvertisementImageRequest
            {
                LeagueId = leagueId,
                AdvertisementId = advertisementId,
                UserId = userId,
                Kind = kind,
            }, cancellationToken);

            AdvertisementImageStorage.TryDeleteManaged(response.PreviousImageUrl);
            return NoContent();
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Guid.Empty;
            return userId;
        }
    }

    public class AdvertisementBody
    {
        public string Name { get; set; } = string.Empty;
        public string AdvertiserName { get; set; } = string.Empty;
        public string? TargetUrl { get; set; }
        public AdvertisementSlot Slot { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UploadAdvertisementImageRequest
    {
        public IFormFile? File { get; set; }
    }
}
