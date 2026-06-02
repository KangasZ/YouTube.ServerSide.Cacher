using Microsoft.AspNetCore.Mvc;
using YouTube.ServerSide.Cacher.Models;
using YouTube.ServerSide.Cacher.Services.CacheServices;
using YouTube.ServerSide.Cacher.Services.DownloadServices;
using YouTube.ServerSide.Cacher.Services.DownloadServices.SiteDownloader;

namespace YouTube.ServerSide.Cacher.Controllers.Api;

[ApiController]
public class WatchController(DownloadManager downloadManager, CacheManager cacheManager)
    : ControllerBase
{
    [HttpGet("/api/watch/youtube/{videoId}")]
    public async Task<IActionResult> Watch([FromRoute] string videoId)
    {
        return await WatchBase(videoId);
    }

    [HttpGet("/w/y/{videoId}")]
    public async Task<IActionResult> WatchUX([FromRoute] string videoId)
    {
        return await WatchBase(videoId);
    }

    private async Task<IActionResult> WatchBase(string videoId)
    {
        if (string.IsNullOrEmpty(videoId))
            return BadRequest();

        var id = YouTubeDownloader.GetVideoId(videoId);
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }
        var downloadEntry = downloadManager.QueueOrGetDownload(SupportedSites.YouTube, videoId);
        if (downloadEntry == null)
        {
            return NotFound();
        }

        await downloadEntry.Task;
        if (downloadEntry.DownloadInformation.Status == StatusEnum.Failed)
        {
            return Problem();
        }
        Response.Headers.CacheControl = "public, max-age=43200, immutable";
        return PhysicalFile(
            cacheManager.GetVideoPath(
                downloadEntry.DownloadInformation.Site,
                downloadEntry.DownloadInformation.SiteId
            ),
            "video/mp4",
            true
        );
    }
}
