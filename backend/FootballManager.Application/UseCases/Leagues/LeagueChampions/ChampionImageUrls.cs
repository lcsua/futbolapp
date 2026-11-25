using System;

namespace FootballManager.Application.UseCases.Leagues.LeagueChampions
{
    /// <summary>
    /// Matches <c>LogoThumbnailService</c>: original <c>abc.jpg</c> has thumb <c>abc.thumb.webp</c>.
    /// </summary>
    public static class ChampionImageUrls
    {
        public const string ThumbSuffix = ".thumb.webp";

        public static string? ThumbUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;
            if (imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return null;

            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absolute))
            {
                if (!absolute.AbsolutePath.Contains("/uploads/", StringComparison.OrdinalIgnoreCase))
                    return null;
                var thumbPath = ReplaceFileNameWithThumb(absolute.AbsolutePath);
                return thumbPath == null ? null : absolute.GetLeftPart(UriPartial.Authority) + thumbPath;
            }

            if (!imageUrl.Contains("/uploads/", StringComparison.OrdinalIgnoreCase))
                return null;

            return ReplaceFileNameWithThumb(imageUrl.StartsWith('/') ? imageUrl : "/" + imageUrl);
        }

        private static string? ReplaceFileNameWithThumb(string absolutePath)
        {
            var slash = absolutePath.LastIndexOf('/');
            if (slash < 0 || slash == absolutePath.Length - 1)
                return null;

            var file = absolutePath[(slash + 1)..];
            if (file.EndsWith(ThumbSuffix, StringComparison.OrdinalIgnoreCase))
                return absolutePath;

            var dot = file.LastIndexOf('.');
            var name = dot > 0 ? file[..dot] : file;
            return absolutePath[..(slash + 1)] + name + ThumbSuffix;
        }
    }
}
