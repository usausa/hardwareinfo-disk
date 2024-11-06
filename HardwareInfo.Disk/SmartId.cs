namespace HardwareInfo.Disk;

#pragma warning disable CA1008
#pragma warning disable CA1028
public enum SmartId : byte
{
    RawReadError = 0x01,
    ThroughputPerformance = 0x02,
    SpinUpTime = 0x03,
    StartStop = 0x04,
    ReallocatedSector = 0x05,
    ReadChannelMargin = 0x06,
    SeekError = 0x07,
    SeekTimePerformance = 0x08,
    PowerOnHours = 0x09,
    SpinRetry = 0x0A,
    RecalibrationRetry = 0x0B,
    PowerCycle = 0x0C,
    SoftReadError = 0x0D,
    PowerRetract = 0xC0,
    CurrentHeliumLevel = 0x16,

    ErrorCorrection = 0xB8,
    ReportedUncorrectableError = 0xBB,

    LoadUnloadCycle = 0xC1,
    Temperature = 0xC2,
    HardwareEccRecovered = 0xC3,
    ReallocationEvent = 0xC4,
    CurrentPendingSector = 0xC5,
    UncorrectableSector = 0xC6,
    UltraDmaCrcError = 0xC7,

    // Vendor specific

    ProgramFail = 0xAB,
    EraseFail = 0xAC,
    AverageBlockErase = 0xAD,
    UnsafeShutdown = 0xAE,

    // TODO Crucial/Micron check
    PercentageLifetimeRemaining = 0xCA,
    TotalHostSectorWrite = 0xF6
}
#pragma warning restore CA1028
#pragma warning restore CA1008
