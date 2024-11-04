namespace HardwareInfo.Disk;

#pragma warning disable CA1008
#pragma warning disable CA1028
public enum SmartId : byte
{
    RawReadErrorRate = 0x01,
    ThroughputPerformance = 0x02,
    SpinUpTime = 0x03,
    StartStopCount = 0x04,
    ReallocatedSectorsCount = 0x05,
    SeekErrorRate = 0x07,
    SeekTimePerformance = 0x08,
    PowerOnHours = 0x09,
    SpinRetryCount = 0x0A,
    DevicePowerCycleCount = 0x0C,
    CurrentHeliumLevel = 0x16,
    LoadUnloadCycleCount = 0xC1,
    Temperature = 0xC2,
    HardwareEccRecovered = 0xC3,
    ReallocationEventCount = 0xC4,
    CurrentPendingSectorCount = 0xC5,
    OffLineScanUncorrectableSectorCount = 0xC6,
    UltraDmaCrcErrorCount = 0xC7,
    DiskShift = 0xDC
}
#pragma warning restore CA1028
#pragma warning restore CA1008
