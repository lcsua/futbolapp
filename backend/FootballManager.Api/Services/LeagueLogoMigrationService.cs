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
        var converted = 0;
        var skipped = 0;
        var failed = 0;

        var teams = await _db.Teams
            .Where(t => t.LeagueId == leagueId)
            .ToListAsync(cancellationToken);

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

        var clubs = await _db.Clubs
            .Where(c => c.LeagueId == leagueId)
            .ToListAsync(cancellationToken);

        foreach (var club in clubs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (DataUrlImageMaterializer.IsDataUrl(club.LogoUrl))
                {
                    var url = await DataUrlImageMaterializer.MaterializeIfDataUrlAsync(
                        club.LogoUrl, leagueId, request, cancellationToken);
                    club.Update(club.Name, url ?? string.Empty);
                    converted++;
                }
                else
                {
                    skipped++;
                }
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
