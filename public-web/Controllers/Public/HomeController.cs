using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PublicWeb.Helpers;
using PublicWeb.Models.Public;
using PublicWeb.Seo;
using PublicWeb.Services.Public;

namespace PublicWeb.Controllers.Public;

public class HomeController : Controller
{
    private readonly IMemoryCache _cache;
    private readonly Web3FormsOptions _web3Forms;
    private readonly SeoUrlBuilder _urls;
    private readonly LeaguePublicService _leagues;

    public HomeController(
        IMemoryCache cache,
        IOptions<Web3FormsOptions> web3Forms,
        SeoUrlBuilder urls,
        LeaguePublicService leagues)
    {
        _cache = cache;
        _web3Forms = web3Forms.Value;
        _urls = urls;
        _leagues = leagues;
    }

    [HttpGet("")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index()
    {
        ViewBag.V2ActiveNav = "home";
        if (!Request.Query.ContainsKey("inicio") && !Request.Query.ContainsKey("todas"))
        {
            var pinned = Request.Cookies[HomeLeaguePreference.PinnedCookie];
            if (HomeLeaguePreference.IsValidPath(pinned))
                return Redirect(HomeLeaguePreference.ToPublicUrl(pinned!));
        }

        PublicWeb.Seo.SeoPageApplicator.Apply(PublicWeb.Seo.SeoCopy.Home(), ViewData, ViewBag);
        var leagues = await _leagues.GetPublicLeaguesAsync();
        var example = leagues.FirstOrDefault(l => l.Slug == "veteranos-de-perico")
            ?? leagues.FirstOrDefault();
        return View("~/Views/V2/Home.cshtml", new HomePageViewModel
        {
            Leagues = leagues,
            ExampleLeague = example
        });
    }

    [HttpPost("liga-inicio")]
    public IActionResult SetDefaultLeague([FromBody] HomeLeaguePreferenceRequest? body)
    {
        var slug = body?.Slug?.Trim();
        HomeLeaguePreference.SetCookie(Response, Request, HomeLeaguePreference.PinnedCookie, slug);
        if (HomeLeaguePreference.IsValidPath(slug))
            HomeLeaguePreference.SetCookie(Response, Request, HomeLeaguePreference.LastCookie, slug);
        return Ok();
    }

    [HttpGet("precios")]
    [ResponseCache(Duration = 1800)]
    public IActionResult Pricing()
    {
        return View("~/Views/Public/Pricing.cshtml");
    }

    [HttpGet("contacto")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Contact()
    {
        ViewBag.V2ActiveNav = "contacto";
        PublicWeb.Seo.SeoPageApplicator.Apply(PublicWeb.Seo.SeoCopy.Contacto(), ViewData, ViewBag);
        return View("~/Views/V2/Contacto.cshtml", new ContactFormViewModel
        {
            AccessKey = _web3Forms.AccessKey?.Trim() ?? "",
            RedirectUrl = _urls.Absolute("/gracias"),
            CaptchaA = Random.Shared.Next(1, 9),
            CaptchaB = Random.Shared.Next(1, 9)
        });
    }

    [HttpGet("gracias")]
    [ResponseCache(Duration = 1800)]
    public IActionResult Thanks()
    {
        ViewBag.V2ActiveNav = "contacto";
        PublicWeb.Seo.SeoPageApplicator.Apply(PublicWeb.Seo.SeoCopy.Gracias(), ViewData, ViewBag);
        return View("~/Views/V2/Gracias.cshtml");
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        return Redirect("/admin");
    }
}
