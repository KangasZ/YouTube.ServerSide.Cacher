using Microsoft.AspNetCore.Mvc;
using YT.Cacher.VideoManager;
using YT.Cacher.YTDownloader;

namespace YT.Cacher.Controllers;

[ApiController]
[Route("status")]
public class StatusController(YoutubeVideoManager videoManager) : ControllerBase
{
    [HttpPost]
    public IActionResult Status([FromQuery] string v)
    {
        if (string.IsNullOrEmpty(v))
            return BadRequest();

        var id = videoManager.GetVideoId(v);

        var information = videoManager.GetDownloadInformation(id);
        return Ok(information);
    }
}
