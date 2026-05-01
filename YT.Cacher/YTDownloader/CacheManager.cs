namespace YT.Cacher.YTDownloader;

public class CacheManager
{
    public string CachePath { get; }
    private readonly ILogger<CacheManager>? logger;

    public CacheManager(IConfiguration configuration, ILogger<CacheManager>? logger = null)
    {
        CachePath = configuration["Paths:CachePath"] ?? "./cache/";
        this.logger = logger;
        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
    }

    public string? TryGetCachedVideoPath(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId)) return null;
        if (!Directory.Exists(CachePath)) return null;

        var match = Directory
            .EnumerateFiles(CachePath, $"{videoId}.webm", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();

        return match is null ? null : Path.GetFullPath(match);
    }

    public int CleanupOlderThan(TimeSpan maxAge)
    {
        if (!Directory.Exists(CachePath)) return 0;

        var cutoff = DateTime.UtcNow - maxAge;
        var deleted = 0;

        foreach (var file in Directory.EnumerateFiles(CachePath, "*.webm", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var info = new FileInfo(file);
                // Use the more recent of CreationTime / LastWriteTime as the "downloaded at" timestamp
                var timestamp = info.CreationTimeUtc > info.LastWriteTimeUtc
                    ? info.CreationTimeUtc
                    : info.LastWriteTimeUtc;

                if (timestamp < cutoff)
                {
                    info.Delete();
                    deleted++;
                    logger?.LogInformation("Deleted cached video {File} (age {Age})", info.Name, DateTime.UtcNow - timestamp);
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
