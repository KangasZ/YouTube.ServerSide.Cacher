using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using YouTube.ServerSide.Cacher.Services.CacheServices;
using YouTube.ServerSide.Cacher.Services.DownloadServices.SiteDownloader;

namespace YouTube.ServerSide.Cacher.Tests.IntegrationTests;

public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    internal YouTubeDownloaderMock YouTubeDownloaderMock;
    internal readonly WebApplicationFactory<Program> applicationFactory;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        applicationFactory = factory;
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

    internal async Task<HttpResponseMessage> ActStatus(string id, HttpClient client)
    {
        var response = await client.GetAsync($"/api/status/youtube/{id}");
        return response;
    }

    internal async Task<HttpResponseMessage> ActQueue(string id, HttpClient client)
    {
        var response = await client.GetAsync($"/api/queue/youtube/{id}");
        return response;
    }

    internal async Task<HttpResponseMessage> ActWatch(string id, HttpClient client)
    {
        var response = await client.GetAsync($"/api/watch/youtube/{id}");
        return response;
    }

    internal async Task CreateDummyVideoFile(string id, int byteSize)
    {
        YouTubeDownloaderMock.writtenFiles.Add($"./cache/YouTube/{id}.mp4");
        await File.WriteAllBytesAsync($"./cache/YouTube/{id}.mp4", new byte[byteSize]);
    }

    internal void DeleteDummyVideoFileIfExists(string id)
    {
        File.Delete($"./cache/YouTube/{id}.mp4");
    }

    internal bool CheckIfVideoFileExists(string id)
    {
        return File.Exists($"./cache/YouTube/{id}.mp4");
    }

    public void Dispose()
    {
        YouTubeDownloaderMock.Dispose();
    }
}
