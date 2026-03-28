namespace HardwareInfo.Disk;

#pragma warning disable CA1819
public interface ISmartNvme : ISmart
{
    byte CriticalWarning { get; }

    short Temperature { get; }

    byte AvailableSpare { get; }

    byte AvailableSpareThreshold { get; }

    byte PercentageUsed { get; }

    ulong DataUnitRead { get; }

    ulong DataUnitWritten { get; }

    ulong HostReadCommands { get; }

    ulong HostWriteCommands { get; }

    ulong ControllerBusyTime { get; }

    ulong PowerCycles { get; }

    ulong PowerOnHours { get; }

    ulong UnsafeShutdowns { get; }

    ulong MediaErrors { get; }

    ulong ErrorInfoLogEntries { get; }

    uint WarningCompositeTemperatureTime { get; }

    uint CriticalCompositeTemperatureTime { get; }

    short[] TemperatureSensors { get; }
}
#pragma warning restore CA1819
