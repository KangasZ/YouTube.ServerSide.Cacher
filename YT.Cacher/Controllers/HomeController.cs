using Microsoft.AspNetCore.Mvc;
using YT.Cacher.YTDownloader;

namespace YT.Cacher.Controllers;

[ApiController]
[Route("")]
public class HomeController(IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        var path = Path.Combine(env.WebRootPath, "HomeIndex.html");
        return PhysicalFile(path, "text/html; charset=utf-8");
    }
}
