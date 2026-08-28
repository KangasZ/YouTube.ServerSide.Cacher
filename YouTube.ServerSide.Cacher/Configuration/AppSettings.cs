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
    public SponsorBlockSettings SponsorBlock { get; set; } = new SponsorBlockSettings();
    public Protection Protection { get; set; } = new Protection();
}

public class AdditionalYtDlpArguments
{
    public string YouTubeArguments { get; set; } = string.Empty;
}

public class SponsorBlockSettings
{
    public bool Enabled { get; set; } = true;

    // https://wiki.sponsor.ajay.app/w/Types#Category
    public string[] Categories { get; set; } = [];
}

public class Protection {
    public bool Enabled { get; set; } = true;
    public string Password { get; set; } = string.Empty;
    public string ApiSigningKey { get; set; } = string.Empty;
    public string CookieSigningKey { get; set; } = string.Empty;
}
