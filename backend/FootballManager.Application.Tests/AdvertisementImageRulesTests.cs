using FootballManager.Application.UseCases.Leagues.AdvertisementImages;

namespace FootballManager.Application.Tests;

public class AdvertisementImageRulesTests
{
    [Fact]
    public void Accepts_jpeg_png_and_webp()
    {
        Assert.Null(AdvertisementImageRules.Validate("banner.jpg", "image/jpeg", 1024));
        Assert.Null(AdvertisementImageRules.Validate("banner.jpeg", "image/jpeg", 1024));
        Assert.Null(AdvertisementImageRules.Validate("banner.png", "image/png", 1024));
        Assert.Null(AdvertisementImageRules.Validate("banner.webp", "image/webp", 1024));
    }

    [Fact]
    public void Rejects_disallowed_format()
    {
        var error = AdvertisementImageRules.Validate("banner.gif", "image/gif", 1024);
        Assert.NotNull(error);
        Assert.Contains("webp", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_file_over_max_size()
    {
        var error = AdvertisementImageRules.Validate(
            "banner.png",
            "image/png",
            AdvertisementImageRules.MaxFileBytes + 1);

        Assert.Equal("Image size must be up to 5 MB.", error);
    }

    [Fact]
    public void Rejects_empty_file()
    {
        Assert.Equal("An image file is required.", AdvertisementImageRules.Validate("banner.png", "image/png", 0));
    }
}
