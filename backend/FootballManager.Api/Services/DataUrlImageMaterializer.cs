using System.Text.RegularExpressions;
using SixLabors.ImageSharp;

namespace FootballManager.Api.Services;

/// <summary>
/// Persists data:image/...;base64,... payloads as real files under wwwroot/uploads,
/// and generates .thumb.webp beside them.
/// </summary>
public static class DataUrlImageMaterializer
{
    private static readonly Regex DataUrlRegex = new(
        @"^data:(?<mime>image/(?<subtype>png|jpeg|jpg|gif|webp));base64,(?<data>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsDataUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// If <paramref name="imageUrl"/> is a data-URL, writes original + thumb and returns the public URL.
    /// Otherwise returns the input unchanged.
    /// </summary>
    public static async Task<string?> MaterializeIfDataUrlAsync(
        string? imageUrl,
        Guid leagueId,
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsDataUrl(imageUrl))
            return imageUrl;

        var match = DataUrlRegex.Match(imageUrl!);
        if (!match.Success)
            throw new InvalidOperationException("Unsupported data-URL image format.");

        var subtype = match.Groups["subtype"].Value.ToLowerInvariant();
        var ext = subtype is "jpeg" or "jpg" ? ".jpg" : $".{subtype}";
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(match.Groups["data"].Value);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Invalid base64 image data.", ex);
        }

        if (bytes.Length == 0)
            throw new InvalidOperationException("Empty image data.");
        if (bytes.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Image size must be up to 5 MB.");

        // Validate it is a real image before writing.
        await using (var probe = new MemoryStream(bytes))
        {
            var info = await Image.IdentifyAsync(probe, cancellationToken)
                ?? throw new InvalidOperationException("Could not decode image.");
            _ = info;
        }

        var relativeDir = Path.Combine("uploads", "leagues", leagueId.ToString(), "images");
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativeDir);
        Directory.CreateDirectory(rootDir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(rootDir, fileName);
        await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);

        try
        {
            await LogoThumbnailService.GenerateThumbFromFileAsync(fullPath, cancellationToken);
        }
        catch
        {
            // Thumbnail is best-effort; original file still counts.
        }

        var relativeUrl = $"/{relativeDir.Replace("\\", "/")}/{fileName}";
        return $"{request.Scheme}://{request.Host}{relativeUrl}";
    }
}
