using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using YouTube.ServerSide.Cacher.Configuration;
using YouTube.ServerSide.Cacher.Models;
using YouTube.ServerSide.Cacher.Services.CacheServices;

namespace YouTube.ServerSide.Cacher.Services.DownloadServices.SiteDownloader;

public interface IYouTubeDownloader : ISiteDownloader;

public class YouTubeDownloader(
    PathManager pathManager,
    ILogger<YouTubeDownloader> logger,
    CacheManager cacheManager,
    AppSettings appSettings
) : IYouTubeDownloader
{
    public Task<int> DownloadVideo(
        DownloadInformation downloadInformation,
        CancellationToken cancellationToken = default
    )
    {
        return DownloadYT(downloadInformation, cancellationToken);
    }

    private static readonly Regex CodecRegex = new(
        @"codecs=(?<videoCodec>.+?)/(?<audioCodec>.+?)",
        RegexOptions.Compiled
    );

    private static readonly Regex ProgressRegex = new(
        @"progressPercent=(?<progress>.+?)\s",
        RegexOptions.Compiled
    );

    private static readonly Regex ActualSizeRegex = new(
        @"actualSize=(?<actualSize>\d+(?:\.\d+)?)\s",
        RegexOptions.Compiled
    );

    private static readonly Regex EstimatedSizeRegex = new(
        @"estimatedSize=(?<estimatedSize>.+?)\s",
        RegexOptions.Compiled
    );

    private static readonly Regex SpeedRegex = new(@"speed=(?<speed>.+?)\s", RegexOptions.Compiled);

    private static readonly Regex EtaRegex = new(@"eta=(?<eta>.+?)\s", RegexOptions.Compiled);

    private async Task<int> DownloadYT(
        DownloadInformation information,
        CancellationToken cancellationToken = default
    )
    {
        var exportPath = cacheManager.GetVideoPath(information.Site, information.SiteId);
        var progressTemplate = new List<string>
        {
            "download:[customDownloadStats] ",
            "codecs=%(info.vcodec)s/%(info.acodec)s ",
            "progressPercent=%(progress._percent)s ",
            "actualSize=%(progress.total_bytes)s ",
            "estimatedSize=%(progress.total_bytes_estimate)s ",
            "speed=%(progress.speed)s ",
            "eta=%(progress.eta)s ",
        };
        var args = new List<string>
        {
            $"--js-runtimes deno:\"{pathManager.DenoPath}\"",
            "--ignore-config",
            "-N 16",
            "--audio-quality 0",
            "-f \"bv*[height<=1080]+ba\"",
            $"-o \"{exportPath}\"",
            "-t mp4",
            "--progress-delta 0.5",
            $"--progress-template \"{string.Join("", progressTemplate)}\"", // full=%(progress)s info=%(info)s\
            $"\"https://youtube.com/watch?v={information.SiteId}\"",
        };

        if (!string.IsNullOrWhiteSpace(appSettings.AdditionalYtDlpArguments.YouTubeArguments))
        {
            args.Add(appSettings.AdditionalYtDlpArguments.YouTubeArguments);
        }

        if (pathManager.CookiesExist)
        {
            args.Add($"--cookies {pathManager.CookiesPath}");
        }

        using var ytdlpProcess = new Process
        {
            StartInfo =
            {
                FileName = pathManager.YtdlpPath,
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

        information.EndTime = DateTime.UtcNow;
        return ytdlpProcess.ExitCode;
    }

    private void ParseStatsFromLine(DownloadInformation information, string line)
    {
        if (!line.StartsWith("[customDownloadStats]"))
        {
            return;
        }

        information.Status = StatusEnum.Downloading;

        // var codecMatch = CodecRegex.Match(line);
        var progressMatch = ProgressRegex.Match(line);
        var actualSizeMatch = ActualSizeRegex.Match(line);
        var estimatedSizeMatch = EstimatedSizeRegex.Match(line);
        var speedMatch = SpeedRegex.Match(line);
        var etaMatch = EtaRegex.Match(line);

        if (progressMatch.Success)
        {
            var progressStr = progressMatch.Groups["progress"].Value;
            if (double.TryParse(progressStr, out var progressDouble))
            {
                information.TotalProgress = progressDouble;
            }
        }

        if (speedMatch.Success)
        {
            var speedStr = speedMatch.Groups["speed"].Value;
            if (double.TryParse(speedStr, out var speedDouble))
            {
                information.CurrentDownloadSpeed = speedDouble;
            }
        }

        if (etaMatch.Success)
        {
            var etaStr = etaMatch.Groups["eta"].Value;
            if (double.TryParse(etaStr, out var etaDouble))
            {
                information.Eta = etaDouble;
            }
        }

        if (actualSizeMatch.Success)
        {
            var actualSizeStr = actualSizeMatch.Groups["actualSize"].Value;
            if (long.TryParse(actualSizeStr, out var actualSizeLong))
            {
                information.TotalSize = actualSizeLong;
            }
        }
        else if (estimatedSizeMatch.Success)
        {
            var estimatedSizeStr = estimatedSizeMatch.Groups["estimatedSize"].Value;
            if (double.TryParse(estimatedSizeStr, out var estimatedSizeDouble))
            {
                information.TotalSize = (long)estimatedSizeDouble;
            }
        }
    }

    private static readonly Regex VideoIdFormatRegex = new(
        @"^[A-Za-z0-9_\-]{11}$",
        RegexOptions.Compiled
    );

    private static readonly Regex VideoIdQueryRegex = new(
        @"[?&]v=([A-Za-z0-9_\-]{11})",
        RegexOptions.Compiled
    );

    private static readonly Regex VideoIdPathRegex = new(
        @"(?:youtu\.be/|youtube(?:-nocookie)?\.com/(?:shorts|embed|live|v)/)([A-Za-z0-9_\-]{11})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static string? ExtractVideoId(string url)
    {
        var m = VideoIdQueryRegex.Match(url);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? ExtractVideoIdAlt(string url)
    {
        var m = VideoIdPathRegex.Match(url);
        return m.Success ? m.Groups[1].Value : null;
    }

    public static string? GetVideoId(string urlOrId)
    {
        var id = ExtractVideoId(urlOrId) ?? ExtractVideoIdAlt(urlOrId) ?? urlOrId.Trim();
        return VideoIdFormatRegex.IsMatch(id) ? id : null;
    }
}
