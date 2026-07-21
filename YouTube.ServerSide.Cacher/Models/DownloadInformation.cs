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
    public double Eta => EtaArray.Average();
    public int Quality { get; set; } = 1080;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatusEnum Status { get; set; } = StatusEnum.Queued;

    [JsonIgnore]
    public int EtaCount { get; set; } = 0;
    [JsonIgnore]
    public static readonly int ArrayCount = 5;

    [JsonIgnore] public double[] EtaArray { get; set; } = [0d,0,0,0,0];

}
