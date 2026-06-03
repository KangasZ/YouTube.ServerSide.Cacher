using Microsoft.AspNetCore.Mvc;
using YouTube.ServerSide.Cacher.Models;
using YouTube.ServerSide.Cacher.Services.DownloadServices;
using YouTube.ServerSide.Cacher.Services.DownloadServices.SiteDownloader;

namespace YouTube.ServerSide.Cacher.Controllers.Api;

[ApiController]
[Route("api/status")]
public class StatusController(DownloadManager downloadManager) : ControllerBase
{
    [HttpGet("youtube/{videoId}")]
    public IActionResult Status([FromRoute] string videoId)
    {
        if (string.IsNullOrEmpty(videoId))
            return BadRequest();

        var id = YouTubeDownloader.GetVideoId(videoId);
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }
        var dlInfo = downloadManager.QueueOrGetDownload(SupportedSites.YouTube, id, false);
        if (dlInfo is null)
        {
            return NotFound();
        }
        return Ok(dlInfo.DownloadInformation);
    }
}
