using YouTube.ServerSide.Cacher.Configuration;
using YouTube.ServerSide.Cacher.Models;

namespace YouTube.ServerSide.Cacher.Services.CacheServices;

public class CacheManager
{
    public string CachePath { get; }
    private readonly ILogger<CacheManager>? logger;
    private Dictionary<SupportedSites, string> sitesBasePaths = new();

    public CacheManager(AppSettings appSettings, ILogger<CacheManager> logger)
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

        var deleteNewerThan = DateTime.UtcNow - maxAge;

        try
        {
            CleanupDirectory(CachePath, deleteNewerThan);
        }
        catch (Exception e)
        {
            logger.LogError("Failed to cleanup base cache directory: {error}", e);
        }

        var directories = Directory.EnumerateDirectories(CachePath);
        foreach (var directory in directories)
        {
            var directoryinfo = new DirectoryInfo(directory);
            if (directoryinfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                logger.LogInformation("Not cleaning up directory for it is a link: {name}", directoryinfo.Name);
            }
            CleanupDirectory(directory, deleteNewerThan);
        }

        return 0;
    }

    // TODO: Add some sane return here.
    private int CleanupDirectory(string path, DateTime deleteNewerThan)
    {
        var files = Directory.EnumerateFiles(path);
        foreach (var filePath in files)
        {
            var fileInfo = new FileInfo(filePath);
            logger.LogInformation("{name}, {size}, {lastmodified}", fileInfo.Name, fileInfo.Length, fileInfo.LastWriteTimeUtc);
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                logger.LogInformation("Not deleting file for it is a link: {name}", fileInfo.Name);
            }
            var lastModifiedAt = fileInfo.LastWriteTimeUtc;
            if (lastModifiedAt < deleteNewerThan)
            {
                try
                {
                    fileInfo.Delete();
                    logger.LogInformation("File deleted: {name} {lastmodified}", fileInfo.Name, fileInfo.LastWriteTimeUtc);
                }
                catch (Exception e)
                {
                    logger.LogError("Failed to delete file, perhaps it is in use?: {error}", e);
                }
            }
            else
            {
                logger.LogInformation("File not deleted: {name} {lastmodified}", fileInfo.Name, fileInfo.LastWriteTimeUtc);
            }
        }

        return 0;
    }
}
