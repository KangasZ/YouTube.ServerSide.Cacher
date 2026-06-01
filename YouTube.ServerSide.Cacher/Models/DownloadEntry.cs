namespace YouTube.ServerSide.Cacher.Models;

public record DownloadEntry(Task<int> Task, DownloadInformation DownloadInformation);
