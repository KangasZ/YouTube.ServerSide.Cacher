using YouTube.ServerSide.Cacher.Models;

namespace YouTube.ServerSide.Cacher.ExtensionMethods;

public static class FormatBytes
{
    public static string FormatIntoReaadableBytes(this long longBase)
    {
        var numBytes = (double)longBase;
        string[] units = { "B", "KiB", "MiB", "GiB", "TiB" };
        var i = 0;
        while (numBytes >= 1024 && i < units.Length - 1)
        {
            numBytes /= 1024;
            i++;
        }

        return $"{numBytes:0.##}{units[i]}";
    }
}
