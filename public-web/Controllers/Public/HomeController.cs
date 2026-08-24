using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PublicWeb.Helpers;

namespace PublicWeb.Controllers.Public;

public class HomeController : Controller
{
    private readonly IMemoryCache _cache;

    public HomeController(IMemoryCache cache)
    {
        _cache = cache;
    }

    [HttpGet("")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Index()
    {
        ViewBag.V2ActiveNav = "home";
        if (!Request.Query.ContainsKey("inicio") && !Request.Query.ContainsKey("todas"))
        {
            var target = HomeLeaguePreference.Resolve(
                Request.Cookies[HomeLeaguePreference.PinnedCookie],
                Request.Cookies[HomeLeaguePreference.LastCookie]);
            if (target != null)
                return Redirect(HomeLeaguePreference.ToPublicUrl(target));
        }

        PublicWeb.Seo.SeoPageApplicator.Apply(PublicWeb.Seo.SeoCopy.Home(), ViewData, ViewBag);
        return View("~/Views/V2/Home.cshtml");
    }

    [HttpGet("precios")]
    [ResponseCache(Duration = 1800)]
    public IActionResult Pricing()
    {
        return View("~/Views/Public/Pricing.cshtml");
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        return Redirect("/admin");
    }
}
