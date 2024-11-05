namespace HardwareInfo.Disk;

public sealed class PartitionInfo
{
    private readonly List<DriveInfo> drives = [];

    public uint Index { get; internal set; }

    public string DeviceId { get; internal set; } = default!;

    public string Name { get; internal set; } = default!;

    public ulong Size { get; internal set; }

    public IReadOnlyList<DriveInfo> Drives => drives;

    internal void AddDrive(DriveInfo drive)
    {
        drives.Add(drive);
    }
}
