using Microsoft.AspNetCore.Mvc;
using YT.Cacher.VideoManager;
using YT.Cacher.YTDownloader;

namespace YT.Cacher.Controllers;

[ApiController]
[Route("watch")]
public class WatchController(YoutubeVideoManager youtubeVideoManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Watch([FromQuery] string v)
    {
        if (string.IsNullOrEmpty(v))
            return BadRequest();

        var id = youtubeVideoManager.GetVideoId(v);
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }
        var cachePath = await youtubeVideoManager.GetVideoPath(id);
        if (cachePath == null)
        {
            return BadRequest();
        }
        Response.Headers.CacheControl = "public, max-age=43200, immutable";
        return PhysicalFile(cachePath, "video/mp4", true);
    }
}
