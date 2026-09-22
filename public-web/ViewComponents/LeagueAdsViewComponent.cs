using Microsoft.AspNetCore.Mvc;
using PublicWeb.Models.Public;
using PublicWeb.Services.Public;

namespace PublicWeb.ViewComponents;

public class LeagueAdsViewComponent : ViewComponent
{
    private readonly LeaguePublicService _leagues;

    public LeagueAdsViewComponent(LeaguePublicService leagues)
    {
        _leagues = leagues;
    }

    public async Task<IViewComponentResult> InvokeAsync(string slot)
    {
        if (string.IsNullOrWhiteSpace(slot))
            return Content(string.Empty);

        var slug = ViewContext.ViewBag.LeagueSlug as string
            ?? (ViewContext.ViewBag.League as LeagueViewModel)?.Slug;
        if (string.IsNullOrWhiteSpace(slug))
            return Content(string.Empty);

        var ads = await _leagues.GetAdvertisementsAsync(slug);
        var matches = ads
            .Where(ad => string.Equals(ad.Slot, slot, StringComparison.OrdinalIgnoreCase))
            .Where(ad => !string.IsNullOrWhiteSpace(ad.DesktopImageUrl) || !string.IsNullOrWhiteSpace(ad.MobileImageUrl))
            .ToList();

        if (matches.Count == 0)
            return Content(string.Empty);

        return View("~/Views/Shared/V2/_LeagueAds.cshtml", matches);
    }
}
