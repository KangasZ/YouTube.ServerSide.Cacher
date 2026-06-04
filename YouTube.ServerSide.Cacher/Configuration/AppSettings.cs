namespace YouTube.ServerSide.Cacher.Configuration;

public class Paths
{
    public string CachePath { get; set; } = string.Empty;
    public string CookiePath { get; set; } = string.Empty;
    public string? YtDlpPath { get; set; }
    public string? DenoPath { get; set; }
}

public class AppSettings
{
    public Paths Paths { get; set; } = new Paths();
    public AdditionalYtDlpArguments AdditionalYtDlpArguments { get; set; } =
        new AdditionalYtDlpArguments();
}

public class AdditionalYtDlpArguments
{
    public string YouTubeArguments { get; set; } = string.Empty;
}
