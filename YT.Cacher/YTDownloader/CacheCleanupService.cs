namespace YT.Cacher.YTDownloader;

public class CacheCleanupService(CacheManager cacheManager, ILogger<CacheCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once at startup, then every 24h
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                var deleted = cacheManager.CleanupOlderThan(MaxAge);
                logger.LogInformation("Cache cleanup complete: {Count} file(s) removed", deleted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cache cleanup failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
