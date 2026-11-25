using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.UseCases.Leagues.LeagueChampions;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace FootballManager.Api.Services
{
    public static class ChampionPhotoStorage
    {
        public const int MaxEdgePx = 1920;

        public static string RelativeDirectory(Guid leagueId, Guid championId)
            => Path.Combine("uploads", "leagues", leagueId.ToString(), "champions", championId.ToString());

        public static async Task<string> SaveAsync(
            Guid leagueId,
            Guid championId,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            var relativeDir = RelativeDirectory(leagueId, championId);
            var rootDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativeDir);
            Directory.CreateDirectory(rootDir);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(rootDir, fileName);

            await using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(fs, cancellationToken);
            }

            try
            {
                await using var probe = File.OpenRead(fullPath);
                _ = await Image.IdentifyAsync(probe, cancellationToken)
                    ?? throw new InvalidOperationException("Could not decode image.");
            }
            catch (InvalidOperationException)
            {
                AdvertisementImageStorage.TryDeleteFile(fullPath);
                throw;
            }
            catch
            {
                AdvertisementImageStorage.TryDeleteFile(fullPath);
                throw new InvalidOperationException("Could not decode image.");
            }

            try
            {
                using var image = await Image.LoadAsync(fullPath, cancellationToken);
                if (image.Width > MaxEdgePx || image.Height > MaxEdgePx)
                {
                    image.Mutate(ctx => ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(MaxEdgePx, MaxEdgePx),
                        Mode = ResizeMode.Max
                    }));
                    await image.SaveAsync(fullPath, cancellationToken);
                }
            }
            catch
            {
                AdvertisementImageStorage.TryDeleteFile(fullPath);
                throw new InvalidOperationException("Could not decode image.");
            }

            try
            {
                await LogoThumbnailService.GenerateThumbFromFileAsync(fullPath, cancellationToken);
            }
            catch
            {
                // The original remains usable if the thumbnail cannot be written.
            }

            return $"/{relativeDir.Replace('\\', '/')}/{fileName}";
        }

        public static void TryDeleteWithThumb(string? imageUrl)
        {
            AdvertisementImageStorage.TryDeleteManaged(imageUrl);
            AdvertisementImageStorage.TryDeleteManaged(ChampionImageUrls.ThumbUrl(imageUrl));
        }
    }
}
