using Microsoft.AspNetCore.Mvc;

namespace YouTube.ServerSide.Cacher.Controllers.Web;

[ApiController]
[Route("")]
public class HomeController(IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        var path = Path.Combine(env.WebRootPath, "HomeIndex.html");
        return PhysicalFile(path, "text/html");
    }

    [HttpGet("/HomeIndex.js")]
    public IActionResult IndexJs()
    {
        var path = Path.Combine(env.WebRootPath, "HomeIndex.js");
        return PhysicalFile(path, "text/javascript");
    }

    [HttpGet("/HomeIndex.css")]
    public IActionResult IndexCss()
    {
        var path = Path.Combine(env.WebRootPath, "HomeIndex.css");
        return PhysicalFile(path, "text/css");
    }
}
