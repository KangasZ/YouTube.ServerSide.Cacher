using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace YT.Cacher.YTDownloader;

public record DownloadInformation
{
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public string TotalSize { get; set; } = "0B";
    public double VideoProgress { get; set; }
    public string VideoSize { get; set; } = "0B";
    public double AudioProgress { get; set; }
    public string AudioSize { get; set; } = "0B";
    public string? VideoDownloadSpeed { get; set; }
    public string? AudioDownloadSpeed { get; set; }
    public StatusEnum Status { get; set; } = StatusEnum.Queued;
}

public record DownloadEntry(Task<int> Task, DownloadInformation DownloadInformation);

public class Downloader
{
    private readonly CacheManager cacheManager;
    private readonly ILogger<Downloader> logger;
    private readonly PathManager pathManager;
    private readonly ConcurrentDictionary<string, DownloadEntry> inFlight = new();

    public Downloader(
        PathManager pathManager,
        ILogger<Downloader> logger,
        CacheManager cacheManager
    )
    {
        this.pathManager = pathManager;
        this.logger = logger;
        this.cacheManager = cacheManager;
    }

    public Task<int> DownloadVideo(string videoId, CancellationToken cancellationToken = default)
    {
        // GetOrAdd ensures only one Task per id is created.
        var entry = inFlight.GetOrAdd(
            videoId,
            _ =>
            {
                logger.LogInformation("Queueing new download for {Id}", videoId);

                var stats = new DownloadInformation()
                {
                    StartTime = DateTime.UtcNow,
                    VideoProgress = 0,
                    AudioProgress = 0,
                };
                // Run on background thread so concurrent callers don't share cancellation.
                var t = Task.Run(
                    () => DownloadYT(videoId, stats, cancellationToken),
                    cancellationToken
                );

                // Remove from dictionary when done (success, fail, or cancel).
                var a = t.ContinueWith(
                    completed =>
                    {
                        inFlight.TryRemove(videoId, out var p);
                        logger.LogInformation(
                            "Download for {Id} finished, removed from in-flight set",
                            videoId
                        );
                    },
                    TaskScheduler.Default
                );

                return new DownloadEntry(t, stats);
            }
        );

        logger.LogInformation(
            "Joining download task for {Id} (progress={Status})",
            videoId,
            entry.DownloadInformation
        );

        return entry.Task;
    }

    public bool TryGetStats(string id, out DownloadInformation? stats)
    {
        if (inFlight.TryGetValue(id, out var entry))
        {
            stats = entry.DownloadInformation;
            return true;
        }
        stats = null;
        return false;
    }

    private async Task<int> DownloadYT(
        string id,
        DownloadInformation information,
        CancellationToken cancellationToken = default
    )
    {
        var args = new List<string>
        {
            "--ignore-config",
            $"--cookies {pathManager.CookiesPath}",
            "-N 8",
            "--audio-quality 0",
            "-f \"bv*[ext=mp4][height<=1080]+ba[ext=webm]\"",
            $"-o \"{cacheManager.CachePath}%(id)s.%(ext)s\"",
            "--merge-output-format webm",
            "--progress-template \"download:[dlstats] kind=%(info.vcodec)s/%(info.acodec)s fid=%(info.format_id)s pct=%(progress._percent_str)s size=%(progress._total_bytes_str)s speed=%(progress._speed_str)s eta=%(progress._eta_str)s\"",
            $"\"https://youtube.com/watch?v={id}\"",
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
                StandardErrorEncoding = Encoding.UTF8,
            },
            EnableRaisingEvents = true,
        };

        ytdlpProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;
            logger.LogInformation("yt-dlp: {Line}", e.Data);
            ParseStatsFromLine(information, e.Data);
        };
        ytdlpProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
                return;
            logger.LogError("yt-dlp: {Line}", e.Data);
        };

        logger.LogInformation(
            "Starting yt-dlp {File} {Args}",
            ytdlpProcess.StartInfo.FileName,
            ytdlpProcess.StartInfo.Arguments
        );

        ytdlpProcess.Start();
        ytdlpProcess.BeginOutputReadLine();
        ytdlpProcess.BeginErrorReadLine();

        await ytdlpProcess.WaitForExitAsync(cancellationToken);

        logger.LogInformation("yt-dlp exited with code {Code}", ytdlpProcess.ExitCode);
        if (ytdlpProcess.ExitCode != 0)
        {
            information.Status = StatusEnum.Failed;
        }
        else
        {
            information.Status = StatusEnum.Success;
        }
        return ytdlpProcess.ExitCode;
    }

    private static readonly Regex StatsRegex = new(
        @"^\[dlstats\]\s+kind=(?<vc>\S+)/(?<ac>\S+)\s+fid=(?<fid>\S+)\s+pct=\s*(?<pct>[\d.]+)%\s+size=\s*(?<size>\S+)\s+speed=\s*(?<speed>\S+)\s+eta=\s*(?<eta>\S+)",
        RegexOptions.Compiled
    );

    private void ParseStatsFromLine(DownloadInformation information, string line)
    {
        var m = StatsRegex.Match(line);
        if (!m.Success)
            return;

        var kind =
            m.Groups["vc"].Value == "none" ? "audio"
            : m.Groups["ac"].Value == "none" ? "video"
            : "muxed";

        if (kind == "video")
        {
            if (
                double.TryParse(
                    m.Groups["pct"].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var pct
                )
            )
                information.VideoProgress = pct;
            information.VideoSize = m.Groups["size"].Value;
            information.VideoDownloadSpeed = m.Groups["speed"].Value;
            information.Status = StatusEnum.DownloadingVideo;
        }
        if (kind == "audio")
        {
            if (
                double.TryParse(
                    m.Groups["pct"].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var pct
                )
            )
                information.AudioProgress = pct;
            information.AudioSize = m.Groups["size"].Value;
            information.AudioDownloadSpeed = m.Groups["speed"].Value;
            information.Status = StatusEnum.DownloadingAudio;
        }
    }
}
