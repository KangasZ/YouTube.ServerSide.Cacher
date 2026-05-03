using System.Text.RegularExpressions;
using YT.Cacher.YTDownloader;

namespace YT.Cacher.VideoManager;

public class YoutubeVideoManager(CacheManager cacheManager, Downloader downloader)
{
    private DownloadInformation FinishedInformation = new DownloadInformation()
    {
        StartTime = DateTime.UtcNow,
        AudioProgress = 100,
        VideoProgress = 100,
    };

    private DownloadInformation StartedInformation = new DownloadInformation()
    {
        StartTime = DateTime.UtcNow,
        AudioProgress = 0,
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

    public string GetVideoId(string urlOrId)
    {
        var id = ExtractVideoId(urlOrId) ?? urlOrId;
        return id;
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
            };
        }
        return StartedInformation with { Status = StatusEnum.Unknown };
    }

    public DownloadInformation QueueDownload(string videoId)
    {
        var cachePath = cacheManager.TryGetCachedVideoPath(videoId);
        if (cachePath is not null)
            return FinishedInformation with { Status = StatusEnum.Cached };

        _ = Task.Run(() => downloader.DownloadVideo(videoId));
        return StartedInformation with { Status = StatusEnum.Queued };
    }

    private static string? ExtractVideoId(string url)
    {
        var match = Regex.Match(url, @"[?&]v=([A-Za-z0-9_\-]{11})");
        return match.Success ? match.Groups[1].Value : null;
    }
}
