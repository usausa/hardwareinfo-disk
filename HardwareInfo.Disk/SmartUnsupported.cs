namespace HardwareInfo.Disk;

internal sealed class SmartUnsupported : ISmart
{
    public static SmartUnsupported Default { get; } = new();

    public bool LastUpdate => false;

    public bool Update() => false;
}
