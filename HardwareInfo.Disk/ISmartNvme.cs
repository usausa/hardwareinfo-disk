namespace HardwareInfo.Disk;

#pragma warning disable CA1819
public interface ISmartNvme : ISmart
{
    public byte CriticalWarning { get; }

    public short Temperature { get; }

    public byte AvailableSpare { get; }

    public byte AvailableSpareThreshold { get; }

    public byte PercentageUsed { get; }

    public ulong DataUnitRead { get; }

    public ulong DataUnitWritten { get; }

    public ulong HostReadCommands { get; }

    public ulong HostWriteCommands { get; }

    public ulong ControllerBusyTime { get; }

    public ulong PowerCycles { get; }

    public ulong PowerOnHours { get; }

    public ulong UnsafeShutdowns { get; }

    public ulong MediaErrors { get; }

    public ulong ErrorInfoLogEntries { get; }

    public uint WarningCompositeTemperatureTime { get; }

    public uint CriticalCompositeTemperatureTime { get; }

    public short[] TemperatureSensors { get; }
}
#pragma warning restore CA1819
