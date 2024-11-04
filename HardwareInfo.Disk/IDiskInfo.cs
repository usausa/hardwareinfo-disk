namespace HardwareInfo.Disk;

public interface IDiskInfo : IDisposable
{
    public string DeviceId { get; }

    public int Index { get; }

    public ulong Size { get; }

    public string PnpDeviceId { get; }

    public string Status { get; }

    public string Model { get; }

    public string SerialNumber { get; }

    public string FirmwareRevision { get; }

    public bool Removable { get; }

    public BusType BusType { get; }

    public SmartType SmartType { get; }

    public ISmart Smart { get; }
}
