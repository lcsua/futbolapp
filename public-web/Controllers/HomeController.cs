using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PublicWeb.Models;

namespace PublicWeb.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("/error/{statusCode:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCode(int statusCode)
    {
        // Preserve original status (e.g. 404) for SEO / clients.
        Response.StatusCode = statusCode;
        if (statusCode == 404)
            return View("NotFound");

        return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

