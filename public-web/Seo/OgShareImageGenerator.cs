using Microsoft.Extensions.Caching.Memory;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PublicWeb.Seo;

public sealed class OgShareImageGenerator
{
    public const int Width = 1200;
    public const int Height = 630;

    private static readonly Color Navy = Color.ParseHex("0F172A");
    private static readonly Color White = Color.ParseHex("F8FAFC");
    private static readonly Color Muted = Color.ParseHex("E2E8F0");

    private readonly FontFamily _family;
    private readonly IMemoryCache _cache;
    private readonly byte[]? _bgBytes;

    public OgShareImageGenerator(IWebHostEnvironment env, IMemoryCache cache)
    {
        _cache = cache;
        _family = LoadFamily(env);
        _bgBytes = LoadBackgroundBytes(env);
    }

    public byte[] Render(string kind, string divisionName, string leagueName, string? seasonName)
    {
        var headline = PublicShareLinks.Headline(kind);
        var division = Clip($"División {Clip(divisionName, 28)}", 36);
        var league = Clip(leagueName, 64);
        var season = Clip(seasonName ?? "", 40);
        var key = $"og4|{headline}|{division}|{league}|{season}";

        return _cache.GetOrCreate(key, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(12);
            return Draw(headline, division, league, season);
        })!;
    }

    private byte[] Draw(string headline, string division, string league, string season)
    {
        using var image = new Image<Rgba32>(Width, Height, Navy);
        image.Mutate(ctx =>
        {
            DrawBackground(ctx);
            DrawRightScrim(ctx);

            var title = _family.CreateFont(64, FontStyle.Bold);
            var divFont = _family.CreateFont(42, FontStyle.Bold);
            var body = _family.CreateFont(26, FontStyle.Regular);
            var small = _family.CreateFont(22, FontStyle.Regular);

            const float x = 560;
            DrawShadowed(ctx, headline, title, White, x, 70);
            DrawShadowed(ctx, division, divFont, White, x, 155);

            var leagueOptions = new RichTextOptions(body)
            {
                Origin = new PointF(x, 240),
                WrappingLength = 580,
                LineSpacing = 1.15f
            };
            ctx.DrawText(leagueOptions, league, Muted);

            if (!string.IsNullOrWhiteSpace(season))
                DrawShadowed(ctx, season, small, Muted, x, 330);
        });

        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder { Quality = 82 });
        return ms.ToArray();
    }

    private void DrawBackground(IImageProcessingContext ctx)
    {
        if (_bgBytes == null || _bgBytes.Length == 0)
            return;

        using var bg = Image.Load<Rgba32>(_bgBytes);
        bg.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(Width, Height),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Left
        }));
        ctx.DrawImage(bg, new Point(0, 0), 1f);
    }

    private static void DrawRightScrim(IImageProcessingContext ctx)
    {
        var brush = new LinearGradientBrush(
            new PointF(420, 0),
            new PointF(820, 0),
            GradientRepetitionMode.None,
            new ColorStop(0f, Color.FromRgba(15, 23, 42, 0)),
            new ColorStop(1f, Color.FromRgba(15, 23, 42, 150)));
        ctx.Fill(brush, new RectangularPolygon(0, 0, Width, Height));
    }

    private static void DrawShadowed(
        IImageProcessingContext ctx,
        string text,
        Font font,
        Color color,
        float x,
        float y)
    {
        ctx.DrawText(text, font, Color.FromRgba(0, 0, 0, 150), new PointF(x + 2, y + 2));
        ctx.DrawText(text, font, color, new PointF(x, y));
    }

    private static byte[]? LoadBackgroundBytes(IWebHostEnvironment env)
    {
        foreach (var path in new[]
        {
            System.IO.Path.Combine(env.ContentRootPath, "Seo", "Assets", "og-share-bg.jpg"),
            System.IO.Path.Combine(AppContext.BaseDirectory, "Seo", "Assets", "og-share-bg.jpg")
        })
        {
            if (File.Exists(path))
                return File.ReadAllBytes(path);
        }

        return null;
    }

    private static FontFamily LoadFamily(IWebHostEnvironment env)
    {
        var collection = new FontCollection();
        var embedded = System.IO.Path.Combine(env.ContentRootPath, "Seo", "Fonts", "Inter-Bold.ttf");
        if (File.Exists(embedded))
            return collection.Add(embedded);

        var output = System.IO.Path.Combine(AppContext.BaseDirectory, "Seo", "Fonts", "Inter-Bold.ttf");
        if (File.Exists(output))
            return collection.Add(output);

        foreach (var name in new[] { "Segoe UI", "Inter", "DejaVu Sans", "Liberation Sans", "Arial", "FreeSans", "Ubuntu" })
        {
            if (SystemFonts.TryGet(name, out var family))
                return family;
        }

        return SystemFonts.Families.First();
    }

    private static string Clip(string? value, int max)
    {
        var t = (value ?? "").Trim();
        if (t.Length <= max) return t;
        return t[..(max - 1)].TrimEnd() + "…";
    }
}
