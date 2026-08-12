using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace FootballManager.Api.Services;

/// <summary>
/// Generates compact WebP thumbnails next to uploaded logos.
/// Naming: original <c>abc.png</c> → thumb <c>abc.thumb.webp</c>.
/// </summary>
public static class LogoThumbnailService
{
    public const int MaxEdgePx = 128;
    public const string ThumbSuffix = ".thumb.webp";

    public static string ThumbFileName(string originalFileName)
    {
        var name = Path.GetFileNameWithoutExtension(originalFileName);
        return name + ThumbSuffix;
    }

    /// <summary>
    /// Derives the public thumb URL for a stored logo URL (local uploads only).
    /// Returns null for data-URLs, empty values, or external hosts we do not control.
    /// </summary>
    public static string? DeriveThumbUrl(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl)) return null;
        if (logoUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;

        if (Uri.TryCreate(logoUrl, UriKind.Absolute, out var absolute))
        {
            if (!absolute.AbsolutePath.Contains("/uploads/", StringComparison.OrdinalIgnoreCase))
                return null;
            var thumbPath = ReplaceFileNameWithThumb(absolute.AbsolutePath);
            if (thumbPath == null) return null;
            return absolute.GetLeftPart(UriPartial.Authority) + thumbPath;
        }

        if (!logoUrl.Contains("/uploads/", StringComparison.OrdinalIgnoreCase))
            return null;

        return ReplaceFileNameWithThumb(logoUrl.StartsWith('/') ? logoUrl : "/" + logoUrl);
    }

    public static string? LocalThumbPathBeside(string originalFullPath)
    {
        if (string.IsNullOrWhiteSpace(originalFullPath)) return null;
        var dir = Path.GetDirectoryName(originalFullPath);
        if (string.IsNullOrWhiteSpace(dir)) return null;
        return Path.Combine(dir, ThumbFileName(Path.GetFileName(originalFullPath)));
    }

    public static async Task GenerateThumbFromFileAsync(string originalFullPath, CancellationToken cancellationToken = default)
    {
        var thumbPath = LocalThumbPathBeside(originalFullPath)
            ?? throw new InvalidOperationException("Could not resolve thumbnail path.");

        await using var input = File.OpenRead(originalFullPath);
        using var image = await Image.LoadAsync(input, cancellationToken);
        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(MaxEdgePx, MaxEdgePx),
            Mode = ResizeMode.Max
        }));

        await image.SaveAsWebpAsync(thumbPath, new WebpEncoder { Quality = 82 }, cancellationToken);
    }

    public static async Task GenerateThumbFromStreamAsync(Stream input, string thumbFullPath, CancellationToken cancellationToken = default)
    {
        if (input.CanSeek) input.Position = 0;
        using var image = await Image.LoadAsync(input, cancellationToken);
        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(MaxEdgePx, MaxEdgePx),
            Mode = ResizeMode.Max
        }));
        await image.SaveAsWebpAsync(thumbFullPath, new WebpEncoder { Quality = 82 }, cancellationToken);
        if (input.CanSeek) input.Position = 0;
    }

    /// <summary>
    /// Creates missing thumbs under a league images directory. Returns counts.
    /// </summary>
    public static async Task<(int Created, int Skipped, int Failed)> BackfillDirectoryAsync(
        string imagesDirectory,
        CancellationToken cancellationToken = default)
    {
        var created = 0;
        var skipped = 0;
        var failed = 0;

        if (!Directory.Exists(imagesDirectory))
            return (0, 0, 0);

        foreach (var file in Directory.EnumerateFiles(imagesDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(file);
            if (name.EndsWith(ThumbSuffix, StringComparison.OrdinalIgnoreCase))
            {
                skipped++;
                continue;
            }

            var ext = Path.GetExtension(file);
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp"))
            {
                skipped++;
                continue;
            }

            var thumb = LocalThumbPathBeside(file)!;
            if (File.Exists(thumb))
            {
                skipped++;
                continue;
            }

            try
            {
                await GenerateThumbFromFileAsync(file, cancellationToken);
                created++;
            }
            catch
            {
                failed++;
            }
        }

        return (created, skipped, failed);
    }

    private static string? ReplaceFileNameWithThumb(string absolutePath)
    {
        var file = Path.GetFileName(absolutePath);
        if (string.IsNullOrWhiteSpace(file)) return null;
        if (file.EndsWith(ThumbSuffix, StringComparison.OrdinalIgnoreCase))
            return absolutePath.Replace('\\', '/');

        var dir = absolutePath[..absolutePath.LastIndexOf('/')];
        var name = Path.GetFileNameWithoutExtension(file);
        return $"{dir}/{name}{ThumbSuffix}";
    }
}
