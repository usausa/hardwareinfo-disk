namespace HardwareInfo.Disk;

#pragma warning disable CA1819
public interface ISmartNvme : ISmart
{
    public byte AvailableSpare { get; }

    public byte AvailableSpareThreshold { get; }

    public ulong ControllerBusyTime { get; }

    public uint CriticalCompositeTemperatureTime { get; }

    public byte CriticalWarning { get; }

    public ulong DataUnitRead { get; }

    public ulong DataUnitWritten { get; }

    public ulong ErrorInfoLogEntryCount { get; }

    public ulong HostReadCommands { get; }

    public ulong HostWriteCommands { get; }

    public ulong MediaErrors { get; }

    public byte PercentageUsed { get; }

    public ulong PowerCycle { get; }

    public ulong PowerOnHours { get; }

    public short Temperature { get; }

    public short[] TemperatureSensors { get; }

    public ulong UnsafeShutdowns { get; }

    public uint WarningCompositeTemperatureTime { get; }
}
#pragma warning restore CA1819
