namespace HardwareInfo.Disk;

public sealed class DriveInfo
{
    public PartitionInfo Partition { get; internal set; } = default!;

    public string DeviceId { get; internal set; } = default!;

    public string Name { get; internal set; } = default!;

    public string FileSystem { get; internal set; } = default!;

    public ulong Size { get; internal set; }

    public ulong FreeSpace { get; internal set; }
}
