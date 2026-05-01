using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace YT.Cacher.YTDownloader;

public class Downloader
{
    private readonly CacheManager cacheManager;
    private readonly ILogger<Downloader> logger;
    private readonly PathManager pathManager;
    private readonly ConcurrentDictionary<string, Task<int>> inFlight = new();

    public Downloader(PathManager pathManager, ILogger<Downloader> logger, CacheManager cacheManager)
    {
        this.pathManager = pathManager;
        this.logger = logger;
        this.cacheManager = cacheManager;
    }

    public Task<int> DownloadVideo(string videoUrl, CancellationToken cancellationToken = default)
    {
        var id = ExtractVideoId(videoUrl) ?? videoUrl;

        // GetOrAdd ensures only one Task per id is created.
        var task = inFlight.GetOrAdd(id, _ =>
        {
            logger.LogInformation("Queueing new download for {Id}", id);

            // Run on background thread so concurrent callers don't share cancellation.
            var t = Task.Run(() => DownloadYT(videoUrl, cancellationToken), cancellationToken);

            // Remove from dictionary when done (success, fail, or cancel).
            var a = t.ContinueWith(completed =>
            {
                inFlight.TryRemove(id, out var p);
                logger.LogInformation("Download for {Id} finished, removed from in-flight set", id);
            }, TaskScheduler.Default);

            return t;
        });

        if (!ReferenceEquals(task, inFlight.GetValueOrDefault(id)) || task.IsCompleted)
            logger.LogInformation("Awaiting existing in-flight download for {Id}", id);
        else
            logger.LogInformation("Joining download task for {Id}", id);

        return task;
    }

    private static string? ExtractVideoId(string url)
    {
        var match = Regex.Match(url, @"[?&]v=([A-Za-z0-9_\-]{11})");
        return match.Success ? match.Groups[1].Value : null;
    }

    private async Task<int> DownloadYT(string videoUrl, CancellationToken cancellationToken = default)
    {
        var args = new List<string>
        {
            "--ignore-config",
            $"--cookies {pathManager.CookiesPath}",
            "--audio-quality 0",
            "-f \"bv[ext=webm]+ba[ext=webm]\"",
            $"-o \"{cacheManager.CachePath}%(id)s.%(ext)s\"",
            "-S res:1080,res:720,webm",
            $"\"{videoUrl}\""
        };

        using var ytdlpProcess = new Process
        {
            StartInfo =
            {
                FileName = pathManager.YtdlPath,
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            },
            EnableRaisingEvents = true
        };

        ytdlpProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) logger.LogInformation("yt-dlp: {Line}", e.Data);
        };
        ytdlpProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) logger.LogWarning("yt-dlp: {Line}", e.Data);
        };

        logger.LogInformation("Starting yt-dlp {File} {Args}",
            ytdlpProcess.StartInfo.FileName, ytdlpProcess.StartInfo.Arguments);

        ytdlpProcess.Start();
        ytdlpProcess.BeginOutputReadLine();
        ytdlpProcess.BeginErrorReadLine();

        await ytdlpProcess.WaitForExitAsync(cancellationToken);

        logger.LogInformation("yt-dlp exited with code {Code}", ytdlpProcess.ExitCode);
        return ytdlpProcess.ExitCode;
    }
}
