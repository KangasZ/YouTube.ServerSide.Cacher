namespace YouTube.ServerSide.Cacher.Models;

public class FileInformation
{
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeInBytes { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime Created { get; set; }
}
