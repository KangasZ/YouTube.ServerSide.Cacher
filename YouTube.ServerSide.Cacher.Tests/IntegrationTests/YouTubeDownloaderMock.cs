using YouTube.ServerSide.Cacher.Models;
using YouTube.ServerSide.Cacher.Services.DownloadServices.SiteDownloader;

namespace YouTube.ServerSide.Cacher.Tests.IntegrationTests;

public class YouTubeDownloaderMock : IYouTubeDownloader
{
    public Task<int> DownloadVideo(DownloadInformation downloadInformation, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}
