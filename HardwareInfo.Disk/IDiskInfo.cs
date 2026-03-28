namespace HardwareInfo.Disk;

public interface IDiskInfo : IDisposable
{
    uint Index { get; }

    string DeviceId { get; }

    string PnpDeviceId { get; }

    string Status { get; }

    string Model { get; }

    string SerialNumber { get; }

    string FirmwareRevision { get; }

    ulong Size { get; }

    uint PhysicalBlockSize { get; }

    uint BytesPerSector { get; }

    uint SectorsPerTrack { get; } // TotalSectors / TotalTracks

    uint TracksPerCylinder { get; } // TotalTracks / TotalCylinders

    uint TotalHeads { get; }

    ulong TotalCylinders { get; }

    ulong TotalTracks { get; }

    ulong TotalSectors { get; }

    uint Partitions { get; }

    bool Removable { get; }

    BusType BusType { get; }

    SmartType SmartType { get; }

    ISmart Smart { get; }
}
