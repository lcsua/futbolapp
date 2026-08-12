using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Api.Services;

public class LeagueLogoMigrationService
{
    private readonly FootballManagerDbContext _db;

    public LeagueLogoMigrationService(FootballManagerDbContext db)
    {
        _db = db;
    }

    public async Task<(int Converted, int Skipped, int Failed)> MaterializeDataUrlLogosAsync(
        Guid leagueId,
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        var teams = await _db.Teams
            .Where(t => t.LeagueId == leagueId)
            .ToListAsync(cancellationToken);

        var converted = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var team in teams)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var changed = false;

            try
            {
                if (DataUrlImageMaterializer.IsDataUrl(team.LogoUrl))
                {
                    var url = await DataUrlImageMaterializer.MaterializeIfDataUrlAsync(
                        team.LogoUrl, leagueId, request, cancellationToken);
                    team.UpdateDetails(
                        team.PrimaryColor,
                        team.SecondaryColor,
                        url ?? string.Empty,
                        team.Email,
                        team.PhotoUrl);
                    changed = true;
                }

                if (DataUrlImageMaterializer.IsDataUrl(team.PhotoUrl))
                {
                    var url = await DataUrlImageMaterializer.MaterializeIfDataUrlAsync(
                        team.PhotoUrl, leagueId, request, cancellationToken);
                    team.UpdateDetails(
                        team.PrimaryColor,
                        team.SecondaryColor,
                        team.LogoUrl,
                        team.Email,
                        url ?? string.Empty);
                    changed = true;
                }

                if (changed) converted++;
                else skipped++;
            }
            catch
            {
                failed++;
            }
        }

        if (converted > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return (converted, skipped, failed);
    }
}
