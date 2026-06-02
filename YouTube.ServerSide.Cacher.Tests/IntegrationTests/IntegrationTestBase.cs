using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using YouTube.ServerSide.Cacher.Services.DownloadServices.SiteDownloader;

namespace YouTube.ServerSide.Cacher.Tests.IntegrationTests;

public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    internal readonly WebApplicationFactory<Program> applicationFactory;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        applicationFactory = factory;
    }

    public HttpClient Client()
    {
        var client = applicationFactory.CreateClient();
        return client;
    }

    public HttpClient ClientWithSiteDownloaderMock()
    {
        var client = applicationFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IYouTubeDownloader, YouTubeDownloaderMock>();
                });
            })
            .CreateClient();
        return client;
    }
}
