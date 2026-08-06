using FootballManager.Application.Exceptions;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Helpers;

public static class SeasonGuard
{
    public static void EnsureOpen(Season season)
    {
        if (!season.IsActive)
            throw new BusinessException(
                "This season is closed. Reopen it before changing setup, divisions, teams or fixtures.");
    }
}
