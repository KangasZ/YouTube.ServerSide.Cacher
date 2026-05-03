using Microsoft.AspNetCore.Mvc;
using YT.Cacher.VideoManager;

namespace YT.Cacher.Controllers;

[ApiController]
[Route("queue")]
public class QueueController(YoutubeVideoManager youtubeVideoManager) : ControllerBase
{
    [HttpPost]
    public IActionResult Queue([FromQuery] string v)
    {
        if (string.IsNullOrEmpty(v))
            return BadRequest();

        var id = youtubeVideoManager.GetVideoId(v);
        return Accepted(youtubeVideoManager.QueueDownload(id));
    }
}
