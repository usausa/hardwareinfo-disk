namespace HardwareInfo.Disk;

using System.Runtime.InteropServices;

internal sealed unsafe class SafeNativeMemoryHandle : SafeHandle
{
    public SafeNativeMemoryHandle(nuint byteCount)
        : base(IntPtr.Zero, true)
    {
        SetHandle((IntPtr)NativeMemory.Alloc(byteCount));
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    public void* Pointer => (void*)handle;

    protected override bool ReleaseHandle()
    {
        NativeMemory.Free((void*)handle);
        return true;
    }
}
