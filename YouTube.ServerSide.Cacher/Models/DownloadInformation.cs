using System.Text.Json.Serialization;

namespace YouTube.ServerSide.Cacher.Models;

public record DownloadInformation
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SupportedSites Site { get; set; }
    public string SiteId { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public double CurrentDownloadSpeed { get; set; }
    public long TotalSize { get; set; }
    public double TotalProgress { get; set; }
    public double Eta { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatusEnum Status { get; set; } = StatusEnum.Queued;
}
