using YouTube.ServerSide.Cacher.Models;
using YouTube.ServerSide.Cacher.Services.CacheServices;
using YouTube.ServerSide.Cacher.Services.DownloadServices.SiteDownloader;

namespace YouTube.ServerSide.Cacher.Tests.IntegrationTests;

public class YouTubeDownloaderMock(CacheManager cacheManager) : IYouTubeDownloader, IDisposable
{
    private List<string> writtenFiles = new List<string>();
    public List<DownloadInformation> downloads = new List<DownloadInformation>();
    private TimeSpan threadWaitTime = TimeSpan.FromSeconds(1);
    private bool shouldMakeFile = true;
    private bool shouldSucceed = true;
    public async Task<int> DownloadVideo(DownloadInformation downloadInformation, CancellationToken cancellationToken = default)
    {
        downloads.Add(downloadInformation);
        Thread.Sleep(threadWaitTime);
        if (shouldMakeFile)
        {
            await WriteDummyFile(downloadInformation);
        }

        if (shouldSucceed)
        {
            downloadInformation.Status = StatusEnum.Success;
            downloadInformation.EndTime = DateTime.UtcNow;
            downloadInformation.TotalSize = "100B";
            return 0;
        }
        else
        {
            downloadInformation.Status = StatusEnum.Failed;
            return 1;
        }
    }

    public void SetupMock(TimeSpan? threadWaitTime = null, bool? shouldSucceed = null, bool? shouldMakeFile = null)
    {
        this.threadWaitTime = threadWaitTime ?? this.threadWaitTime ;
        this.shouldSucceed = shouldSucceed ?? this.shouldSucceed ;
        this.shouldMakeFile = shouldMakeFile ?? this.shouldMakeFile;
    }

    public async Task WriteDummyFile(DownloadInformation downloadInformation)
    {
        var path = cacheManager.GetVideoPath(downloadInformation.Site, downloadInformation.SiteId);
        await File.WriteAllBytesAsync(path, new byte[5000000]);
        writtenFiles.Add(path);
    }

    public void Dispose()
    {
        foreach (var writtenFile in writtenFiles)
        {
            File.Delete(writtenFile);
        }
    }
}
