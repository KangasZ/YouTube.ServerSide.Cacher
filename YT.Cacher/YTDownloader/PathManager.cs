namespace YT.Cacher.YTDownloader;

public class PathManager
{
    public string CookiesPath { get; }

    public readonly string DenoPath =
        OperatingSystem.IsWindows() ? "deno.exe" : "deno";

    public readonly string FfmpegPath =
        OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";

    public readonly string YtdlPath = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp";

    public PathManager(IConfiguration configuration)
    {
        CookiesPath = configuration["Paths:CookiePath"] ?? "./cookies/cookies.txt";
    }
}
