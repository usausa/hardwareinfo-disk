namespace HardwareInfo.Disk;

#pragma warning disable CA1711
#pragma warning disable CA1815
public struct SmartAttribute
{
    public byte Id { get; set; }

    public short Flags { get; set; }

    public byte CurrentValue { get; set; }

    public byte WorstValue { get; set; }

    public ulong RawValue { get; set; }
}
#pragma warning restore CA1815
#pragma warning restore CA1711
