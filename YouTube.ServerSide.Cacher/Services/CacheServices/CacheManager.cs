using YouTube.ServerSide.Cacher.Configuration;
using YouTube.ServerSide.Cacher.Models;

namespace YouTube.ServerSide.Cacher.Services.CacheServices;

public class CacheManager
{
    public string CachePath { get; }
    private readonly ILogger<CacheManager>? logger;
    private Dictionary<SupportedSites, string> sitesBasePaths = new();

    public CacheManager(AppSettings appSettings, ILogger<CacheManager>? logger = null)
    {
        // Dependencies
        this.logger = logger;

        // Setup Base Path
        var appsettingsCachePath = appSettings.Paths.CachePath;
        if (string.IsNullOrEmpty(appsettingsCachePath))
        {
            throw new Exception(
                "Appsettings Cache path unset. Set this in appsettings or environment variables."
            );
        }
        CachePath = Path.GetFullPath(appsettingsCachePath);

        if (!Directory.Exists(CachePath))
        {
            Directory.CreateDirectory(CachePath);
        }

        // Setup Sites Paths
        foreach (var site in (SupportedSites[])Enum.GetValues(typeof(SupportedSites)))
        {
            if (site == SupportedSites.Default)
            {
                continue;
            }
            var fullSitePath = Path.Combine(CachePath, site.ToString());
            if (!Directory.Exists(fullSitePath))
            {
                Directory.CreateDirectory(fullSitePath);
            }
            sitesBasePaths.Add(site, fullSitePath);
        }
    }

    public string GetVideoPath(SupportedSites site, string videoId)
    {
        var sitePathFound = sitesBasePaths.TryGetValue(site, out var sitePath);
        if (!sitePathFound || string.IsNullOrWhiteSpace(sitePath))
            throw new Exception("Video path error");

        var videoIdWithExt = $"{videoId}.mp4";
        var fullVideoPath = Path.Combine(sitePath, videoIdWithExt);
        return fullVideoPath;
    }

    public FileInformation? GetFileInformation(SupportedSites site, string videoId)
    {
        var path = GetVideoPath(site, videoId);
        if (Path.Exists(path))
        {
            var fileInfo = new FileInformation();
            var baseInfo = new FileInfo(path);

            fileInfo.FileExtension = baseInfo.Extension;
            fileInfo.FileSizeInBytes = baseInfo.Length;
            fileInfo.FileName = baseInfo.Name;
            fileInfo.LastModified = baseInfo.LastWriteTimeUtc;
            fileInfo.Created = baseInfo.CreationTimeUtc;
            fileInfo.FullPath = baseInfo.FullName;

            return fileInfo;
        }
        else
        {
            return null;
        }
    }

    public int CleanupOlderThan(TimeSpan maxAge)
    {
        if (!Directory.Exists(CachePath))
            return 0;

        var cutoff = DateTime.UtcNow - maxAge;
        var deleted = 0;

        foreach (
            var file in Directory.EnumerateFiles(CachePath, "*", SearchOption.TopDirectoryOnly)
        )
        {
            try
            {
                var info = new FileInfo(file);
                // Use the more recent of CreationTime / LastWriteTime as the "downloaded at" timestamp
                var timestamp =
                    info.CreationTimeUtc > info.LastWriteTimeUtc
                        ? info.CreationTimeUtc
                        : info.LastWriteTimeUtc;

                if (timestamp < cutoff)
                {
                    info.Delete();
                    deleted++;
                    logger?.LogInformation(
                        "Deleted cached video {File} (age {Age})",
                        info.Name,
                        DateTime.UtcNow - timestamp
                    );
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to delete cached file {File}", file);
            }
        }

        return deleted;
    }
}
