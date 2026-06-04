using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace YouTube.ServerSide.Cacher.Tests.IntegrationTests.ControllerTests.Web;

public class HomeControllerTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    // Probably unnecessary :')
    private static readonly Regex LinesInHeaadRegex = new(
        @"<head>\n(^.*$\n)+</head>"
        ,RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex HrefOrSrcRegex = new(
        @"(?:href=""(.*)"")|(?:src=""(.*)"")",
        RegexOptions.Compiled | RegexOptions.Multiline);

    [Theory]
    [InlineData("/", "text/html")]
    [InlineData("/HomeIndex.css", "text/css")]
    [InlineData("/HomeIndex.js", "text/javascript")]
    public async Task GetFiles_ReturnsRightFile(string url, string mimeType)
    {
        // Arrange
        var client = ClientWithSiteDownloaderMock();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal($"{mimeType}; charset=utf-8",
            response.Content.Headers.ContentType.ToString());
    }

    [Fact]
    public async Task GetIndexHtml_AllReferencedFilesAccessible()
    {
        var client = ClientWithSiteDownloaderMock();
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var matches = HrefOrSrcRegex.Matches(content);
        var filesToCheck = matches.Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(2, filesToCheck.Count());
        var responses = filesToCheck.Select(file => client.GetAsync($"/{file}"));
        await Task.WhenAll(responses);
        Assert.All(responses, r => Assert.True(r.Result.IsSuccessStatusCode));
    }
}
