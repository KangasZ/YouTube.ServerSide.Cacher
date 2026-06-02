using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace YouTube.ServerSide.Cacher.Tests.IntegrationTests.ControllerTests.Api;

public class QueueTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task HappyPath()
    {
        var response = await Act("zhiiOjLgwrM");
        Assert.True(response.IsSuccessStatusCode);
        // Theres nothing else to do here, queue is a fire and forget
    }

    [Theory]
    [InlineData("0123456789ab")]
    [InlineData("0123456789")]
    public async Task BadRequest_BadId(string id)
    {
        var response = await Act(id);
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task NotFound_NoId()
    {
        var response = await Act("");
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    public async Task<HttpResponseMessage> Act(string id)
    {
        var client = ClientWithSiteDownloaderMock();
        var response = await client.GetAsync($"/api/queue/youtube/{id}");
        return response;
    }
}
