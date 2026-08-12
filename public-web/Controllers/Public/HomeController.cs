using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace PublicWeb.Controllers.Public;

public class HomeController : Controller
{
    private readonly IMemoryCache _cache;

    public HomeController(IMemoryCache cache)
    {
        _cache = cache;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 1800)] // 30 mins
    public IActionResult Index()
    {
        ViewBag.V2ActiveNav = "home";
        ViewData["Title"] = "Inicio";
        ViewData["Description"] = "Resultados, posiciones, partidos y estadísticas de tu torneo.";
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
