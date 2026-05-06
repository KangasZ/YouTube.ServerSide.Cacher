using System.Text.RegularExpressions;
using YT.Cacher.YTDownloader;

namespace YT.Cacher.VideoManager;

public class YoutubeVideoManager(CacheManager cacheManager, Downloader downloader)
{
    private DownloadInformation FinishedInformation = new DownloadInformation()
    {
        AudioProgress = 100,
        TotalProgress = 100,
        VideoProgress = 100,
    };

    private DownloadInformation StartedInformation = new DownloadInformation()
    {
        AudioProgress = 0,
        TotalProgress = 0,
        VideoProgress = 0,
    };

    public async Task<string?> GetVideoPath(string videoId)
    {
        var cachePath = cacheManager.TryGetCachedVideoPath(videoId);
        if (cachePath is null)
        {
            await downloader.DownloadVideo(videoId);
            cachePath = cacheManager.TryGetCachedVideoPath(videoId);
            if (cachePath is null)
                return null;
        }

        return cachePath;
    }

    private static readonly Regex VideoIdFormatRegex = new(
        @"^[A-Za-z0-9_\-]{11}$",
        RegexOptions.Compiled
    );

    public string? GetVideoId(string urlOrId)
    {
        var id = ExtractVideoId(urlOrId) ?? ExtractVideoIdAlt(urlOrId) ?? urlOrId.Trim();
        return VideoIdFormatRegex.IsMatch(id) ? id : null;
    }

    public DownloadInformation GetDownloadInformation(string videoId)
    {
        var statusInformation = downloader.TryGetStats(videoId, out var info);
        if (statusInformation)
        {
            return info;
        }

        var cachePath = cacheManager.TryGetCachedVideoPath(videoId);
        if (cachePath is not null)
        {
            return FinishedInformation with
            {
                Status = StatusEnum.Cached,
                TotalSize = cacheManager.TryGetFileSize(videoId),
                StartTime = DateTime.UtcNow,
            };
        }

        return StartedInformation with
        {
            Status = StatusEnum.Unknown,
        };
    }

    public DownloadInformation QueueDownload(string videoId)
    {
        var cachePath = cacheManager.TryGetCachedVideoPath(videoId);
        if (cachePath is not null)
            return FinishedInformation with
            {
                Status = StatusEnum.Cached,
                StartTime = DateTime.UtcNow,
            };

        _ = Task.Run(() => downloader.DownloadVideo(videoId));
        return StartedInformation with { Status = StatusEnum.Queued, StartTime = DateTime.UtcNow };
    }

    private static readonly Regex VideoIdQueryRegex = new(
        @"[?&]v=([A-Za-z0-9_\-]{11})",
        RegexOptions.Compiled
    );

    private static readonly Regex VideoIdPathRegex = new(
        @"(?:youtu\.be/|youtube(?:-nocookie)?\.com/(?:shorts|embed|live|v)/)([A-Za-z0-9_\-]{11})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static string? ExtractVideoId(string url)
    {
        var m = VideoIdQueryRegex.Match(url);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? ExtractVideoIdAlt(string url)
    {
        var m = VideoIdPathRegex.Match(url);
        return m.Success ? m.Groups[1].Value : null;
    }
}
