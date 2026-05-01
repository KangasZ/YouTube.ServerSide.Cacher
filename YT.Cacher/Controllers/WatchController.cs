using Microsoft.AspNetCore.Mvc;
using YT.Cacher.YTDownloader;

namespace YT.Cacher.Controllers;

[ApiController]
[Route("watch")]
public class WatchController(Downloader dl, CacheManager cacheManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Watch([FromQuery] string v)
    {
        if (string.IsNullOrEmpty(v)) return BadRequest();

        var cachePath = cacheManager.TryGetCachedVideoPath(v);
        if (cachePath is null)
        {
            await dl.DownloadVideo("https://www.youtube.com/watch?v=" + v);
            cachePath = cacheManager.TryGetCachedVideoPath(v);
            if (cachePath is null) return StatusCode(418, "I'm a teacup, not a coffee pot! (unexpected error)");
        }

        return PhysicalFile(cachePath, "video/webm", true);
    }
}
