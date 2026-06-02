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

    private async Task<int> DownloadYT(
        DownloadInformation information,
        CancellationToken cancellationToken = default
    )
    {
        var exportPath = cacheManager.GetVideoPath(information.Site, information.SiteId);
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
            "--progress-template \"download:[dlstats] kind=%(info.vcodec)s/%(info.acodec)s fid=%(info.format_id)s pct=%(progress._percent_str)s size=%(progress._total_bytes_str)s speed=%(progress._speed_str)s eta=%(progress._eta_str)s\"",
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

    private static readonly Regex StatsRegex = new(
        @"^\[dlstats\]\s+kind=(?<vc>\S+)/(?<ac>\S+)\s+fid=(?<fid>\S+)\s+pct=\s*(?<pct>[\d.]+)%\s+size=\s*(?<size>\S+)\s+speed=\s*(?<speed>\S+)\s+eta=\s*(?<eta>\S+)",
        RegexOptions.Compiled
    );

    private void ParseStatsFromLine(DownloadInformation information, string line)
    {
        var m = StatsRegex.Match(line);
        if (!m.Success)
        {
            logger.LogDebug("Unhandled stats line: {Line}", line);
            return;
        }

        var kind =
            m.Groups["vc"].Value == "none" ? "audio"
            : m.Groups["ac"].Value == "none" ? "video"
            : "muxed";

        var parseSuccess = double.TryParse(
            m.Groups["pct"].Value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var pct
        );

        switch (kind)
        {
            case "video":
                information.CurrentDownloadSpeed = m.Groups["speed"].Value;
                information.Status = StatusEnum.DownloadingVideo;
                break;
            case "audio":
                information.CurrentDownloadSpeed = m.Groups["speed"].Value;
                information.Status = StatusEnum.DownloadingAudio;
                break;
            case "muxed":
                information.TotalProgress = parseSuccess ? pct : 0;
                information.TotalSize = m.Groups["size"].Value;
                information.CurrentDownloadSpeed = m.Groups["speed"].Value;
                information.Status = StatusEnum.Downloading;
                break;
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
