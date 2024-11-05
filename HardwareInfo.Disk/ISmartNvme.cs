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

    public ulong DataUnitWrite { get; }

    public ulong HostReadCommand { get; }

    public ulong HostWriteCommand { get; }

    public ulong ControllerBusyTime { get; }

    public ulong PowerCycle { get; }

    public ulong PowerOnHour { get; }

    public ulong UnsafeShutdown { get; }

    public ulong MediaError { get; }

    public ulong ErrorInfoLogEntry { get; }

    public uint WarningCompositeTemperatureTime { get; }

    public uint CriticalCompositeTemperatureTime { get; }

    public short[] TemperatureSensors { get; }
}
#pragma warning restore CA1819
