namespace HardwareInfo.Disk;

using System.Management;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
public static class DiskInfo
{
    public static IDiskInfo[] GetInformation()
    {
        var list = new List<IDiskInfo>();

        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
        foreach (var disk in searcher.Get())
        {
            // TODO
        }

        return list.ToArray();
    }
}

internal sealed class DiscInfoGeneric : IDiskInfo
{
    public ISmart Smart { get; internal set; } = default!;
}
