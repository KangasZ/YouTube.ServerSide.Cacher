using Microsoft.AspNetCore.Mvc;
using YT.Cacher.YTDownloader;

namespace YT.Cacher.Controllers;

[ApiController]
[Route("queue")]
public class QueueController(Downloader dl, CacheManager cacheManager) : ControllerBase
{
    [HttpPost]
    public IActionResult Queue([FromQuery] string v)
    {
        if (string.IsNullOrEmpty(v)) return BadRequest();

        var cachePath = cacheManager.TryGetCachedVideoPath(v);
        if (cachePath is not null) return Ok(new { status = "cached" });

        _ = Task.Run(() => dl.DownloadVideo("https://www.youtube.com/watch?v=" + v));
        return Accepted(new { status = "queued" });
    }
}
