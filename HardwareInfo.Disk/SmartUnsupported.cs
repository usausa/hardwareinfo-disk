namespace HardwareInfo.Disk;

internal sealed class SmartUnsupported : ISmart
{
    public static SmartUnsupported Default { get; } = new();

    public bool Update() => false;
}
