namespace HardwareInfo.Disk;

using Microsoft.Win32.SafeHandles;

// TODO
#pragma warning disable CA1812
#pragma warning disable CA1822
internal sealed class SmartGeneric : ISmartGeneric, IDisposable
{
    private readonly SafeFileHandle handle;

    public bool LastUpdate { get; }

    public SmartGeneric(SafeFileHandle handle)
    {
        this.handle = handle;
        // TODO
        LastUpdate = false;
    }

    public void Dispose()
    {
        handle.Dispose();
    }

    public bool Update() => throw new NotImplementedException();
}
