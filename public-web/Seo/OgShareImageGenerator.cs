using Microsoft.Extensions.Caching.Memory;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PublicWeb.Seo;

public sealed class OgShareImageGenerator
{
    public const int Width = 1200;
    public const int Height = 630;

    private static readonly Color Navy = Color.ParseHex("0F172A");
    private static readonly Color Green = Color.ParseHex("16A34A");
    private static readonly Color GreenDark = Color.ParseHex("15803D");
    private static readonly Color White = Color.ParseHex("F8FAFC");
    private static readonly Color Muted = Color.ParseHex("94A3B8");
    private static readonly Color Ball = Color.ParseHex("E2E8F0");

    private readonly FontFamily _family;
    private readonly IMemoryCache _cache;

    public OgShareImageGenerator(IWebHostEnvironment env, IMemoryCache cache)
    {
        _cache = cache;
        _family = LoadFamily(env);
    }

    public byte[] Render(string kind, string divisionName, string leagueName, string? seasonName)
    {
        var headline = PublicShareLinks.Headline(kind);
        var division = Clip($"División {Clip(divisionName, 28)}", 36);
        var league = Clip(leagueName, 64);
        var season = Clip(seasonName ?? "", 40);
        var key = $"og|{headline}|{division}|{league}|{season}";

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
            ctx.Fill(GreenDark, new RectangularPolygon(0, 0, 18, Height));
            ctx.Fill(Green, new RectangularPolygon(18, 0, 10, Height));

            DrawBall(ctx, 980, 330, 260);

            var brand = _family.CreateFont(28, FontStyle.Bold);
            var title = _family.CreateFont(76, FontStyle.Bold);
            var divFont = _family.CreateFont(48, FontStyle.Bold);
            var body = _family.CreateFont(30, FontStyle.Regular);
            var small = _family.CreateFont(24, FontStyle.Regular);

            ctx.DrawText("MILIGA", brand, Green, new PointF(72, 72));
            ctx.DrawText(headline, title, White, new PointF(72, 150));
            ctx.DrawText(division, divFont, White, new PointF(72, 250));

            var leagueOptions = new RichTextOptions(body)
            {
                Origin = new PointF(72, 360),
                WrappingLength = 720,
                LineSpacing = 1.15f
            };
            ctx.DrawText(leagueOptions, league, Muted);

            if (!string.IsNullOrWhiteSpace(season))
                ctx.DrawText(season, small, Muted, new PointF(72, 540));
        });

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder { CompressionLevel = PngCompressionLevel.BestSpeed });
        return ms.ToArray();
    }

    private static void DrawBall(IImageProcessingContext ctx, float cx, float cy, float r)
    {
        ctx.Fill(Color.FromRgba(22, 163, 74, 40), new EllipsePolygon(cx, cy, r));
        ctx.Fill(Ball, new EllipsePolygon(cx, cy, r * 0.72f));
        ctx.Draw(Navy, r * 0.035f, new EllipsePolygon(cx, cy, r * 0.72f));

        var pent = Pentagon(cx, cy - r * 0.06f, r * 0.16f);
        ctx.Fill(Navy, new Polygon(new LinearLineSegment(pent)));

        for (var i = 0; i < 5; i++)
        {
            var angle = (float)(-Math.PI / 2 + i * 2 * Math.PI / 5);
            var x = cx + MathF.Cos(angle) * r * 0.42f;
            var y = cy + MathF.Sin(angle) * r * 0.42f;
            ctx.Draw(Navy, r * 0.028f, new EllipsePolygon(x, y, r * 0.18f));
        }
    }

    private static PointF[] Pentagon(float cx, float cy, float r)
    {
        var pts = new PointF[5];
        for (var i = 0; i < 5; i++)
        {
            var angle = (float)(-Math.PI / 2 + i * 2 * Math.PI / 5);
            pts[i] = new PointF(cx + MathF.Cos(angle) * r, cy + MathF.Sin(angle) * r);
        }
        return pts;
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
