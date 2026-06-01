using System.Collections.Concurrent;
using YouTube.ServerSide.Cacher.ExtensionMethods;
using YouTube.ServerSide.Cacher.Models;
using YouTube.ServerSide.Cacher.Services.SiteDownloader;
using YouTube.ServerSide.Cacher.Services.YTDownloader;

namespace YouTube.ServerSide.Cacher.Services.VideoManager;

public class DownloadManager
{
    private readonly YouTubeDownloader youtubeDownloader;
    private readonly ConcurrentDictionary<string, DownloadEntry> Downloads = new();
    private readonly CacheManager cacheManager;

    public DownloadManager(YouTubeDownloader youtubeDownloader, CacheManager cacheManager)
    {
        this.cacheManager = cacheManager;
        this.youtubeDownloader = youtubeDownloader;
    }

    public DownloadEntry? QueueOrGetDownload(SupportedSites site, string id)
    {
        // Grab if there's an existing one
        var downloadKey = GetDownloadKey(site, id);
        // Check if its a valid site
        if (site != SupportedSites.YouTube)
        {
            return null;
        }
        // Get downloads key
        var downloadExists = Downloads.TryGetValue(downloadKey, out var downloadValue);

        // Check if the download already exists in cache, regardless of downloads
        var fileInformation = cacheManager.GetFileInformation(site, id);

        // If the file does not exist, and theres a download thats older than an hour, assume its bad and remove dl and requeue
        if (
            fileInformation is null
            && downloadExists
            && downloadValue is not null
            && downloadValue.DownloadInformation.StartTime < DateTime.UtcNow.AddHours(-1)
        )
        {
            Downloads.Remove(downloadKey, out _);
        }
        // If the file does exist, and theres a download, return the download
        else if (fileInformation is not null && downloadExists && downloadValue is not null)
        {
            return downloadValue;
        }
        // If the file does exist, and theres not a download, return some dummy download (with valid info)
        else if (fileInformation is not null && !downloadExists)
        {
            return new DownloadEntry(
                Task.FromResult(0),
                new DownloadInformation()
                {
                    Site = site,
                    SiteId = id,
                    StartTime = fileInformation.Created,
                    EndTime = fileInformation.LastModified,
                    TotalSize = fileInformation.FileSizeInBytes.FormatIntoReaadableBytes(),
                    TotalProgress = 100,
                    Status = StatusEnum.Success,
                }
            );
        }

        // Create or get download task (tbd)
        var download = Downloads.GetOrAdd(
            downloadKey,
            _ =>
            {
                Task<int> task;
                switch (site)
                {
                    case SupportedSites.YouTube:
                        var downloadInfo = new DownloadInformation()
                        {
                            Site = site,
                            SiteId = id,
                            StartTime = DateTime.UtcNow,
                        };
                        task = Task.Run(() => youtubeDownloader.DownloadVideo(downloadInfo));
                        return new DownloadEntry(task, downloadInfo);
                    // Setup failed path
                }

                return null;
            }
        );
        return download;
    }

    private string GetDownloadKey(SupportedSites site, string id)
    {
        return $"{site}~{id}";
    }
}
