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

    public ulong DataUnitWrite { get; }

    public ulong ErrorInfoLogEntry { get; }

    public ulong HostReadCommand { get; }

    public ulong HostWriteCommand { get; }

    public ulong MediaError { get; }

    public byte PercentageUsed { get; }

    public ulong PowerCycle { get; }

    public ulong PowerOnHour { get; }

    public short Temperature { get; }

    public short[] TemperatureSensors { get; }

    public ulong UnsafeShutdown { get; }

    public uint WarningCompositeTemperatureTime { get; }
}
#pragma warning restore CA1819
