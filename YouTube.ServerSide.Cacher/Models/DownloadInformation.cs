using System.Text.Json.Serialization;

namespace YouTube.ServerSide.Cacher.Models;

public record DownloadInformation
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SupportedSites Site { get; set; }
    public string SiteId { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public string? CurrentDownloadSpeed { get; set; }
    public string TotalSize { get; set; } = "0B";
    public double TotalProgress { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatusEnum Status { get; set; } = StatusEnum.Queued;
}
