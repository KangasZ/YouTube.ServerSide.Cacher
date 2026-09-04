using Microsoft.AspNetCore.Mvc;
using YouTube.ServerSide.Cacher.Models;
using YouTube.ServerSide.Cacher.Services.DownloadServices;
using YouTube.ServerSide.Cacher.Services.DownloadServices.SiteDownloader;
using YouTube.ServerSide.Cacher.Services.Protection;

namespace YouTube.ServerSide.Cacher.Controllers.Api;

[ApiController]
[Route("api/queue")]
public class QueueController(DownloadManager downloadManager, IProtectionService protectionService) : ControllerBase
{
    [RequiredCookie]
    [HttpGet("youtube/{videoId}")]
    public IActionResult Queue([FromRoute] string videoId, [FromQuery] int? quality, [FromQuery] bool? forceRedownload)
    {
        if (string.IsNullOrEmpty(videoId))
            return BadRequest();
        var resultQuality = 0;
        switch (quality)
        {
            case 0:
            case null:
                resultQuality = 0;
                break;
            case 720:
                resultQuality = 720;
                break;
            case 1080:
                resultQuality = 1080;
                break;
            case 1440:
                resultQuality = 1440;
                break;
            default:
                return BadRequest();
        }

        var id = YouTubeDownloader.GetVideoId(videoId);
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }

        string? token = null;
        if (protectionService.IsEnabled())
        {
            token = protectionService.GenerateWatchKey(SupportedSites.YouTube, id);
        }

        var dlInfo = downloadManager.QueueOrGetDownload(SupportedSites.YouTube, id, resultQuality, token: token);
        if (dlInfo == null)
        {
            return NotFound();
        }
        return Accepted(dlInfo.DownloadInformation);
    }
}
