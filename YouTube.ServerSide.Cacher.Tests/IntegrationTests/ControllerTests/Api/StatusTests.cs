using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using YouTube.ServerSide.Cacher.Models;

namespace YouTube.ServerSide.Cacher.Tests.IntegrationTests.ControllerTests.Api;

public class StatusTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetStatus_NoDownloadFile_ReturnsNotFound()
    {
        var client = ClientWithSiteDownloaderMock();
        var response = await Act("zhiiOjLgwrM",client);
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
        Assert.Empty(YouTubeDownloaderMock.downloads);
    }

    [Fact]
    public async Task GetStatus_DownloadStarted_ReturnOK_Queued()
    {
        var client = ClientWithSiteDownloaderMock();
        YouTubeDownloaderMock.SetupMock(threadWaitTime: TimeSpan.FromSeconds(5));
        await ActQueue("zhiiOjLgwrM", client);

        var response = await Act("zhiiOjLgwrM", client);
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.Single(YouTubeDownloaderMock.downloads);
        var responseObject = await response.Content.ReadFromJsonAsync<DownloadInformation>();
        Assert.NotNull(responseObject);
        Assert.Equal(SupportedSites.YouTube, responseObject.Site);
        Assert.Equal("zhiiOjLgwrM", responseObject.SiteId);
        Assert.Equal(StatusEnum.Queued, responseObject.Status);
        Assert.Null(responseObject.EndTime);
    }

    [Fact]
    public async Task GetStatus_DownloadFinished_ReturnOK_Success()
    {
        var client = ClientWithSiteDownloaderMock();
        YouTubeDownloaderMock.SetupMock(threadWaitTime: TimeSpan.FromSeconds(0));
        await ActQueue("zhiiOjLgwrM", client);
        Thread.Sleep(TimeSpan.FromSeconds(1));

        var response = await Act("zhiiOjLgwrM", client);
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.Single(YouTubeDownloaderMock.downloads);
        var responseObject = await response.Content.ReadFromJsonAsync<DownloadInformation>();
        Assert.NotNull(responseObject);
        Assert.Equal(SupportedSites.YouTube, responseObject.Site);
        Assert.Equal("zhiiOjLgwrM", responseObject.SiteId);
        Assert.Equal(StatusEnum.Success, responseObject.Status);
        Assert.NotNull(responseObject.EndTime);
        Assert.NotEqual("0B", responseObject.TotalSize);
        Assert.NotEqual(100, responseObject.TotalProgress);
    }

    [Fact]
    public async Task GetStatus_DownloadExistsOnDish_ReturnsCached()
    {

    }

    private async Task<HttpResponseMessage> Act(string id, HttpClient client)
    {
        var response = await client.GetAsync($"/api/status/youtube/{id}");
        return response;
    }

    private async Task<HttpResponseMessage> ActQueue(string id, HttpClient client)
    {
        var response = await client.GetAsync($"/api/queue/youtube/{id}");
        return response;
    }
}
