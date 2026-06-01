using Microsoft.AspNetCore.Mvc;
using YouTube.ServerSide.Cacher.Models;
using YouTube.ServerSide.Cacher.Services.SiteDownloader;
using YouTube.ServerSide.Cacher.Services.VideoManager;

namespace YouTube.ServerSide.Cacher.Controllers.Api;

[ApiController]
[Route("api/queue")]
public class QueueController(DownloadManager downloadManager) : ControllerBase
{
    [HttpGet("youtube/{videoId}")]
    public IActionResult Queue([FromRoute] string videoId)
    {
        if (string.IsNullOrEmpty(videoId))
            return BadRequest();

        var id = YouTubeDownloader.GetVideoId(videoId);
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }

        var dlInfo = downloadManager.QueueOrGetDownload(SupportedSites.YouTube, id);
        return Accepted(dlInfo?.DownloadInformation ?? null);
    }
}
