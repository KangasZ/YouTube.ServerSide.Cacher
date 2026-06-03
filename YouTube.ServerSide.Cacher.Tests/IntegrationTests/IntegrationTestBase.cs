using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using YouTube.ServerSide.Cacher.Services.CacheServices;
using YouTube.ServerSide.Cacher.Services.DownloadServices.SiteDownloader;

namespace YouTube.ServerSide.Cacher.Tests.IntegrationTests;

public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    internal YouTubeDownloaderMock YouTubeDownloaderMock;
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
        var factory = applicationFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IYouTubeDownloader>(sp =>
                    new YouTubeDownloaderMock(sp.GetRequiredService<CacheManager>()));
            });
        });

        var client = factory.CreateClient(); // builds the host
        YouTubeDownloaderMock = (YouTubeDownloaderMock)factory.Services
            .GetRequiredService<IYouTubeDownloader>();
        return client;
    }
}
