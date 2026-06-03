namespace YouTube.ServerSide.Cacher.Models;


public enum StatusEnum
{
    Unknown = -1,
    Queued = 0,
    Downloading = 1,
    DownloadingAudio = 2,
    DownloadingVideo = 3,
    Cached = 4,
    Success = 5,
    Failed = 6,
}
