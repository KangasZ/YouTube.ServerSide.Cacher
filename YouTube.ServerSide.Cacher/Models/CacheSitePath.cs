namespace YouTube.ServerSide.Cacher.Models;

public class CacheSitePath
{
    public SupportedSites Site = SupportedSites.Default;
    public string FullSitePath { get; set; } = string.Empty;
}
