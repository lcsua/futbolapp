using System;
using System.Collections.Generic;
using System.IO;

namespace FootballManager.Application.UseCases.Leagues.AdvertisementImages
{
    public static class AdvertisementImageRules
    {
        /// <summary>
        /// Same cap used by league logo and document image uploads.
        /// </summary>
        public const long MaxFileBytes = 5 * 1024 * 1024;

        public static readonly IReadOnlyCollection<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public static readonly IReadOnlyCollection<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/pjpeg",
            "image/png",
            "image/webp"
        };

        public static string? Validate(string? fileName, string? contentType, long length)
        {
            if (length <= 0)
                return "An image file is required.";
            if (length > MaxFileBytes)
                return "Image size must be up to 5 MB.";

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension) || !Contains(AllowedExtensions, extension))
                return "Allowed image extensions: .jpg, .jpeg, .png, .webp";

            if (string.IsNullOrWhiteSpace(contentType) || !Contains(AllowedContentTypes, contentType))
                return "Only JPEG, PNG and WebP images are allowed.";

            return null;
        }

        private static bool Contains(IReadOnlyCollection<string> values, string candidate)
        {
            foreach (var value in values)
            {
                if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
