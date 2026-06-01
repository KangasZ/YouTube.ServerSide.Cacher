namespace YouTube.ServerSide.Cacher.Models;

public record DownloadInformation
{
    public SupportedSites Site { get; set; }
    public string SiteId { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; } = DateTime.UtcNow;
    public string? CurrentDownloadSpeed { get; set; }
    public string TotalSize { get; set; } = "0B";
    public double TotalProgress { get; set; }
    public StatusEnum Status { get; set; } = StatusEnum.Queued;
}
