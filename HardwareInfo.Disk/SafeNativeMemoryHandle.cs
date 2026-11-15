namespace HardwareInfo.Disk;

using System.Runtime.InteropServices;

#if NET11_0_OR_GREATER
internal sealed class SafeNativeMemoryHandle : SafeHandle
#else
// ReSharper disable once RedundantUnsafeContext
internal sealed unsafe class SafeNativeMemoryHandle : SafeHandle
#endif
{
    public SafeNativeMemoryHandle(nuint byteCount)
        : base(IntPtr.Zero, true)
    {
        SetHandle((IntPtr)NativeMemory.AllocZeroed(byteCount));
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    public void* Pointer => (void*)handle;

    protected override bool ReleaseHandle()
    {
        NativeMemory.Free((void*)handle);
        return true;
    }
}
