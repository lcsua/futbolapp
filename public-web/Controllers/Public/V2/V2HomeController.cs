using Microsoft.AspNetCore.Mvc;

namespace PublicWeb.Controllers.Public.V2;

/// <summary>
/// Permanent redirects from retired /v2 surface onto canonical public URLs.
/// </summary>
[Route("v2")]
public class V2HomeController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return PermanentTo("~/");
    }

    [HttpGet("{**path}")]
    public IActionResult CatchAll(string path)
    {
        var target = string.IsNullOrWhiteSpace(path)
            ? "~/"
            : $"~/{path.TrimStart('/')}";
        return PermanentTo(target);
    }

    private IActionResult PermanentTo(string appRelativePath)
    {
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return RedirectPermanent(Url.Content(appRelativePath)! + qs);
    }
}
