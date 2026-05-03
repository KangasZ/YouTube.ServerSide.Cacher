using Microsoft.AspNetCore.Mvc;
using YT.Cacher.YTDownloader;

namespace YT.Cacher.Controllers;

[ApiController]
[Route("watch-hls")]
public class WatchHlsController(Downloader dl, CacheManager cacheManager) : ControllerBase
{
    // Want to make a way to watch a m3u8/hls stream, however this is a project for another day
    [HttpGet]
    public async Task<IActionResult> Watch([FromQuery] string v)
    {
        throw new NotImplementedException();
    }
}
