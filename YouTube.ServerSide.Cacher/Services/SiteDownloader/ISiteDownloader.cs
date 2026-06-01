using YouTube.ServerSide.Cacher.Models;

namespace YouTube.ServerSide.Cacher.Services.SiteDownloader;

public interface ISiteDownloader
{
    public Task<int> DownloadVideo(
        DownloadInformation downloadInformation,
        CancellationToken cancellationToken = default
    );
}
