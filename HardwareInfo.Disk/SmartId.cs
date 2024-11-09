namespace HardwareInfo.Disk;

#pragma warning disable CA1008
#pragma warning disable CA1028
public enum SmartId : byte
{
    RawReadErrorRate = 0x01,
    ThroughputPerformance = 0x02,
    SpinUpTime = 0x03,
    StartStopCount = 0x04,
    ReallocatedSectorCount = 0x05,
    ReadChannelMargin = 0x06,
    SeekErrorRate = 0x07,
    SeekTimePerformance = 0x08,
    PowerOnHours = 0x09,
    SpinRetryCount = 0x0A,
    RecalibrationRetries = 0x0B,
    PowerCycleCount = 0x0C,
    SoftReadErrorRate = 0x0D,
    PowerOffRetractCount = 0xC0,
    CurrentHeliumLevel = 0x16,

    ErrorCorrectionCount = 0xB8,
    ReportedUncorrectableErrors = 0xBB,

    LoadUnloadCycleCount = 0xC1,
    Temperature = 0xC2,
    HardwareEccRecovered = 0xC3,
    ReallocationEventCount = 0xC4,
    CurrentPendingSectorCount = 0xC5,
    UncorrectableSectorCount = 0xC6,
    UltraDmaCrcErrorCount = 0xC7,

    // Vendor specific

    ProgramFailCount = 0xAB,
    EraseFailCount = 0xAC,
    AverageBlockEraseCount = 0xAD,
    UnexpectedPowerLoss = 0xAE,

    // Crucial/Micron

    PercentageLifetimeRemaining = 0xCA,
    TotalHostSectorWrite = 0xF6
}
#pragma warning restore CA1028
#pragma warning restore CA1008
