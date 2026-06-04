using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using YouTube.ServerSide.Cacher.Models;

namespace YouTube.ServerSide.Cacher.Tests.IntegrationTests.ControllerTests.Api;

public class QueueTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task HappyPath()
    {
        var client = ClientWithSiteDownloaderMock();
        var response = await ActQueue("zhiiOjLgwrM", client);
        Assert.True(response.IsSuccessStatusCode);
        Assert.Single(YouTubeDownloaderMock.downloads);
    }

    [Fact]
    public async Task Queue_SameVideoTwice_ReturnsSuccessSilently()
    {
        var client = ClientWithSiteDownloaderMock();
        var id = "zhiiOjLgwrM";
        var response = await ActQueue(id, client);
        var response2 = await ActQueue(id, client);
        Assert.True(response.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.Single(YouTubeDownloaderMock.downloads);
    }

    [Fact]
    public async Task Queue_VideoAlreadyDownloaded_VideoSinceDeleted_QueuesVideoAgain()
    {
        var client = ClientWithSiteDownloaderMock();
        YouTubeDownloaderMock.SetupMock(threadWaitTime: TimeSpan.FromSeconds(0), shouldMakeFile:true, shouldSucceed:true);
        var id = "awaawaawaaa";
        await ActQueue(id, client);
        Thread.Sleep(TimeSpan.FromSeconds(1));
        Assert.True(CheckIfVideoFileExists(id));
        var responeBeforeDeleted = await ActQueue(id, client);
        Assert.Equal(StatusCodes.Status202Accepted, (int)responeBeforeDeleted.StatusCode);
        var dlInfo = await responeBeforeDeleted.Content.ReadFromJsonAsync<DownloadInformation>();
        Assert.NotNull(dlInfo);
        Assert.Equal(StatusEnum.Success, dlInfo.Status);

        DeleteDummyVideoFileIfExists(id);
        Assert.False(CheckIfVideoFileExists(id));
        var response = await ActQueue(id, client);
        Assert.Equal(StatusCodes.Status202Accepted, (int)response.StatusCode);
        var dlInfo2 = await response.Content.ReadFromJsonAsync<DownloadInformation>();
        Assert.NotNull(dlInfo2);
        Assert.Equal(StatusEnum.Queued, dlInfo2.Status);
    }

    [Theory]
    [InlineData("0123456789ab")]
    [InlineData("0123456789")]
    public async Task BadRequest_BadId(string id)
    {
        var client = ClientWithSiteDownloaderMock();
        var response = await ActQueue(id, client);
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task NotFound_NoId()
    {
        var client = ClientWithSiteDownloaderMock();
        var response = await ActQueue("", client);
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }
}
