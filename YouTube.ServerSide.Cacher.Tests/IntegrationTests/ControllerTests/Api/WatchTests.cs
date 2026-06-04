using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace YouTube.ServerSide.Cacher.Tests.IntegrationTests.ControllerTests.Api;

public class WatchTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Watch_ReturnMp4_Cached()
    {
        var id = Guid.NewGuid().ToString().Substring(0, 11);
        var client = ClientWithSiteDownloaderMock();
        await CreateDummyVideoFile(id, 51200);
        var response = await ActWatch(id, client);
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.Equal("video/mp4", response.Content.Headers.ContentType.MediaType);
        Assert.Empty(YouTubeDownloaderMock.downloads);
        Assert.Equal(51200, response.Content.Headers.ContentLength);
    }

    [Fact]
    public async Task Watch_ReturnMp4_AfterAlreadyQueued()
    {
        var id = Guid.NewGuid().ToString().Substring(0, 11);
        var client = ClientWithSiteDownloaderMock();
        YouTubeDownloaderMock.SetupMock(TimeSpan.FromSeconds(0), true, true);
        var responseQueue = await ActQueue(id, client);
        Assert.Equal(StatusCodes.Status202Accepted, (int)responseQueue.StatusCode);
        Thread.Sleep(TimeSpan.FromSeconds(0.5));
        Assert.Single(YouTubeDownloaderMock.downloads);

        var response = await ActWatch(id, client);
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.Equal("video/mp4", response.Content.Headers.ContentType.MediaType);
        Assert.Equal(5000000, response.Content.Headers.ContentLength);
        Assert.Single(YouTubeDownloaderMock.downloads);
    }

    [Fact]
    public async Task Watch_ReturnMp4_FromScratch()
    {
        var id = Guid.NewGuid().ToString().Substring(0, 11);
        var client = ClientWithSiteDownloaderMock();
        var response = await ActWatch(id, client);
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.Equal("video/mp4", response.Content.Headers.ContentType.MediaType);
        Assert.Equal(5000000, response.Content.Headers.ContentLength);
        Assert.Single(YouTubeDownloaderMock.downloads);
    }

    [Fact]
    public async Task Watch_DownloadFails_Problem()
    {
        var id = Guid.NewGuid().ToString().Substring(0, 11);
        var client = ClientWithSiteDownloaderMock();
        YouTubeDownloaderMock.SetupMock(TimeSpan.FromSeconds(0), false, false);
        var response = await ActWatch(id, client);
        Assert.Equal(StatusCodes.Status500InternalServerError, (int)response.StatusCode);
    }

    [Fact]
    public async Task Watch_DownloadSucceeds_FileDoesNotExist_Problem()
    {
        var id = Guid.NewGuid().ToString().Substring(0, 11);
        var client = ClientWithSiteDownloaderMock();
        YouTubeDownloaderMock.SetupMock(TimeSpan.FromSeconds(0), true, false);
        var response = await ActWatch(id, client);
        Assert.Equal(StatusCodes.Status500InternalServerError, (int)response.StatusCode);
    }

    [Fact]
    public async Task Watch_ReturnMp4_FromScratch_AfterPreviousDownloadGotDeleted()
    {
        var id = Guid.NewGuid().ToString().Substring(0, 11);
        var client = ClientWithSiteDownloaderMock();
        YouTubeDownloaderMock.SetupMock(TimeSpan.FromSeconds(0), true, true);
        var response = await ActWatch(id, client);
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.Equal("video/mp4", response.Content.Headers.ContentType.MediaType);
        Assert.Equal(5000000, response.Content.Headers.ContentLength);
        Assert.Single(YouTubeDownloaderMock.downloads);
        DeleteDummyVideoFileIfExists(id);

        var response2 = await ActWatch(id, client);
        Assert.Equal(StatusCodes.Status200OK, (int)response2.StatusCode);
        Assert.Equal("video/mp4", response2.Content.Headers.ContentType.MediaType);
        Assert.Equal(5000000, response2.Content.Headers.ContentLength);
        Assert.Equal(2, YouTubeDownloaderMock.downloads.Count);
    }
}
