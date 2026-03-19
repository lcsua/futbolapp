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
        return View("~/Views/Public/Home.cshtml");
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
        // For now, redirect to a hypothetical admin/login route or return a view
        // The instructions say "Redirigir o enlazar al panel admin"
        // Let's redirect to /admin or just return an HTML page with a link.
        return Redirect("/admin");
    }
}

