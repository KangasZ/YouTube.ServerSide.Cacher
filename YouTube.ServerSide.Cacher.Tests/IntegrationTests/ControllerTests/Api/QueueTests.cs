using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace YouTube.ServerSide.Cacher.Tests.IntegrationTests.ControllerTests.Api;

public class QueueTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task HappyPath()
    {
        var client = ClientWithSiteDownloaderMock();
        var response = await Act("zhiiOjLgwrM", client);
        Assert.True(response.IsSuccessStatusCode);
        Assert.Single(YouTubeDownloaderMock.downloads);
    }

    [Fact]
    public async Task Queue_SameVideoTwice_ReturnsSuccessSilently()
    {
        var client = ClientWithSiteDownloaderMock();
        var response = await Act("zhiiOjLgwrM", client);
        var response2 = await Act("zhiiOjLgwrM", client);
        Assert.True(response.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.Single(YouTubeDownloaderMock.downloads);
    }

    [Fact]
    public async Task Queue_VideoAlreadyDownloaded_VideoSinceDeleted_QueuesVideoAgain()
    {
        var client = ClientWithSiteDownloaderMock();
        YouTubeDownloaderMock.SetupMock(threadWaitTime: TimeSpan.FromSeconds(0), shouldMakeFile:true, shouldSucceed:true);
        var response = await Act("zhiiOjLgwrM", client);
        Thread.Sleep(TimeSpan.FromSeconds(1));
        Assert.True(File.Exists("./cache/YouTube/zhiiOjLgwrM.mp4"));
    }

    [Theory]
    [InlineData("0123456789ab")]
    [InlineData("0123456789")]
    public async Task BadRequest_BadId(string id)
    {
        var client = ClientWithSiteDownloaderMock();
        var response = await Act(id, client);
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task NotFound_NoId()
    {
        var client = ClientWithSiteDownloaderMock();
        var response = await Act("", client);
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    private async Task<HttpResponseMessage> Act(string id, HttpClient client)
    {
        var response = await client.GetAsync($"/api/queue/youtube/{id}");
        return response;
    }
}
