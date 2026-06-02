using YouTube.ServerSide.Cacher.Configuration;

namespace YouTube.ServerSide.Cacher.Services.CacheServices;

public class PathManager
{
    public string CookiesPath { get; }
    public bool CookiesExist => File.Exists(CookiesPath);

    public readonly string DenoPath = OperatingSystem.IsWindows() ? "deno.exe" : "deno";

    public readonly string FfmpegPath = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";

    public readonly string YtdlpPath = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp";

    public PathManager(AppSettings configuration)
    {
        CookiesPath = configuration.Paths.CookiePath;
        YtdlpPath = string.IsNullOrWhiteSpace(configuration.Paths.YtDlpPath)
            ? YtdlpPath
            : configuration.Paths.YtDlpPath;
        DenoPath = string.IsNullOrWhiteSpace(configuration.Paths.DenoPath)
            ? DenoPath
            : configuration.Paths.DenoPath;
    }
}
