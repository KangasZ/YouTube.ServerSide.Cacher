using System.Collections.Concurrent;
using YouTube.ServerSide.Cacher.ExtensionMethods;
using YouTube.ServerSide.Cacher.Models;
using YouTube.ServerSide.Cacher.Services.CacheServices;
using YouTube.ServerSide.Cacher.Services.DownloadServices.SiteDownloader;

namespace YouTube.ServerSide.Cacher.Services.DownloadServices;

public class DownloadManager
{
    private readonly IYouTubeDownloader youtubeDownloader;
    private readonly ConcurrentDictionary<string, DownloadEntry> Downloads = new();
    private readonly CacheManager cacheManager;

    public DownloadManager(IYouTubeDownloader youtubeDownloader, CacheManager cacheManager)
    {
        this.cacheManager = cacheManager;
        this.youtubeDownloader = youtubeDownloader;
    }

    public DownloadEntry? QueueOrGetDownload(SupportedSites site, string id, bool shouldQueueIfMissing = true)
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

        // If the file does not exist, and theres a download thats in a end state, assume its bad and remove dl and requeue
        StatusEnum[] finishedStates = [StatusEnum.Cached, StatusEnum.Success, StatusEnum.Failed];
        if (
            fileInformation is null
            && downloadExists
            && downloadValue is not null
            && finishedStates.Contains(downloadValue.DownloadInformation.Status)
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
                    Status = StatusEnum.Cached,
                }
            );
        }

        // Create or get download task (tbd)
        if (!shouldQueueIfMissing)
        {
            var dl = Downloads.GetValueOrDefault(downloadKey);
            return dl;
        }
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
