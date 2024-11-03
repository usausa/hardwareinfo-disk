namespace HardwareInfo.Disk;

public interface IDiskInfo
{
    public ISmart Smart { get; }
}
