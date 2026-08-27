using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Enums;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;

namespace FootballManager.Api.Services;

public static class AdvertisementImageStorage
{
    public static string RelativeDirectory(Guid leagueId, Guid advertisementId)
        => Path.Combine("uploads", "leagues", leagueId.ToString(), "advertisements", advertisementId.ToString());

    public static async Task<(string PublicUrl, string FullPath)> SaveAsync(
        HttpRequest request,
        Guid leagueId,
        Guid advertisementId,
        AdvertisementImageKind kind,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
        var relativeDir = RelativeDirectory(leagueId, advertisementId);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativeDir);
        Directory.CreateDirectory(rootDir);

        var fileName = $"{kind.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}{extension}";
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
        catch
        {
            TryDeleteFile(fullPath);
            throw new InvalidOperationException("Could not decode image.");
        }

        var relativeUrl = $"/{relativeDir.Replace("\\", "/")}/{fileName}";
        var publicUrl = $"{request.Scheme}://{request.Host}{relativeUrl}";
        return (publicUrl, fullPath);
    }

    public static void TryDeleteManaged(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        var relativePath = TryGetUploadsRelativePath(imageUrl);
        if (relativePath == null)
            return;

        var wwwroot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
        var fullPath = Path.GetFullPath(Path.Combine(wwwroot, relativePath));
        if (!fullPath.StartsWith(wwwroot, StringComparison.OrdinalIgnoreCase))
            return;

        TryDeleteFile(fullPath);
    }

    public static void TryDeleteFile(string? fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
            return;

        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch
        {
            // Best-effort cleanup of managed uploads.
        }
    }

    private static string? TryGetUploadsRelativePath(string imageUrl)
    {
        string? path = null;
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absolute))
            path = absolute.AbsolutePath;
        else if (imageUrl.Contains("/uploads/", StringComparison.OrdinalIgnoreCase))
            path = imageUrl;

        if (string.IsNullOrWhiteSpace(path))
            return null;

        var marker = "/uploads/";
        var index = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var relative = path[index..].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        if (relative.Contains("..", StringComparison.Ordinal))
            return null;

        return relative;
    }
}
