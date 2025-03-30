namespace ExampleDiskInfo.Models;

using HardwareInfo.Disk;

public sealed class DiskInfoModel
{
    public IDiskInfo Disk { get; }

    public DiskInfoModel(IDiskInfo disk)
    {
        Disk = disk;
        // TODO
    }
}
