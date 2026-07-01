using Microsoft.Extensions.Logging;
using YouTube.ServerSide.Cacher.Configuration;

namespace YouTube.ServerSide.Cacher.Tests.UnitTests.Services.CacheManager;

public class CacheManagerUnitTests
{
    readonly Cacher.Services.CacheServices.CacheManager CacheManagerInstance;
    private readonly string UnitTestCachePath = Directory.GetCurrentDirectory() + "/unittestcache";

    public CacheManagerUnitTests()
    {
        var appsettings = new AppSettings();
        appsettings.Paths.CachePath = UnitTestCachePath;

        //TODO: Potentiall in the future create a dummy logger that sends logged inputs to a list and then we can parse from there instead of just passing in a real logger
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = factory.CreateLogger<Cacher.Services.CacheServices.CacheManager>();
        CacheManagerInstance = new Cacher.Services.CacheServices.CacheManager(appsettings, logger);
    }

    [Fact]
    public async Task CacheManager_DeletesOldFiles()
    {
        var cachePathForDummy = UnitTestCachePath + "/dummy.mp4";
        await WriteDummyFile(cachePathForDummy);
        var info = new FileInfo(cachePathForDummy);
        Assert.True(info.Exists);
        Thread.Sleep(TimeSpan.FromSeconds(1));
        CacheManagerInstance.CleanupOlderThan(TimeSpan.FromSeconds(0.5));
        Thread.Sleep(TimeSpan.FromSeconds(0.1));
        info = new FileInfo(cachePathForDummy);
        Assert.False(info.Exists);
    }

    [Fact]
    public async Task CacheManager_DeletesOldFilesInDirectories()
    {
        var directory = "test";
        Directory.CreateDirectory(UnitTestCachePath + "/test");
        var cachePathForDummy = UnitTestCachePath + "/test/dummy.mp4";
        await WriteDummyFile(cachePathForDummy);
        var info = new FileInfo(cachePathForDummy);
        Assert.True(info.Exists);
        Thread.Sleep(TimeSpan.FromSeconds(1));
        CacheManagerInstance.CleanupOlderThan(TimeSpan.FromSeconds(0.5));
        Thread.Sleep(TimeSpan.FromSeconds(0.1));
        info = new FileInfo(cachePathForDummy);
        Assert.False(info.Exists);
    }

    [Fact]
    public async Task CacheManager_DoesNotDeleteThreeLayersDeep()
    {
        Directory.CreateDirectory(UnitTestCachePath + "/test");
        Directory.CreateDirectory(UnitTestCachePath + "/test/test");
        var cachePathForDummy = UnitTestCachePath + "/test/test/dummy.mp4";
        await WriteDummyFile(cachePathForDummy);
        var info = new FileInfo(cachePathForDummy);
        Assert.True(info.Exists);
        Thread.Sleep(TimeSpan.FromSeconds(1));
        CacheManagerInstance.CleanupOlderThan(TimeSpan.FromSeconds(0.5));
        Thread.Sleep(TimeSpan.FromSeconds(0.1));
        info = new FileInfo(cachePathForDummy);
        Assert.True(info.Exists);
        info.Delete();
    }

    //TODO: Add some tests so it never deletes symlink targets

    private async Task WriteDummyFile(string path)
    {
        await File.WriteAllBytesAsync(path, new byte[10]);
    }
}
