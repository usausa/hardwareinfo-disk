namespace HardwareInfo.Disk;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.NativeMethods;

internal sealed class SmartGeneric : ISmartGeneric, IDisposable
{
    private static readonly int SendCommandInParamsSize = Marshal.SizeOf<SENDCMDINPARAMS>();
    private static readonly int SendCommandOutParamsSize = Marshal.SizeOf<SENDCMDOUTPARAMS>();

    private static readonly int BufferLength = Marshal.SizeOf<ATTRIBUTECMDOUTPARAMS>();

    private static readonly int AttributesSize = Marshal.SizeOf<SMART_ATTRIBUTE>();

    private static readonly int AttributesOffset;

    private readonly SafeFileHandle handle;

    private readonly byte deviceNumber;

    private readonly SafeNativeMemoryHandle buffer;

    private bool disposed;

    public bool LastUpdate { get; private set; }

#pragma warning disable CA1810
    static unsafe SmartGeneric()
    {
        ATTRIBUTECMDOUTPARAMS s = default;
        AttributesOffset = (int)(s.Attributes - (byte*)Unsafe.AsPointer(ref s));
    }
#pragma warning restore CA1810

    public SmartGeneric(SafeFileHandle handle, byte deviceNumber)
    {
        this.handle = handle;
        this.deviceNumber = deviceNumber;
        try
        {
            var parameter = new SENDCMDINPARAMS
            {
                DriveNumber = this.deviceNumber,
                DriveRegs =
                {
                    FeaturesReg = SMART_FEATURES.ENABLE_SMART,
                    CylLowReg = SMART_LBA_MID,
                    CylHighReg = SMART_LBA_HI,
                    CommandReg = ATA_COMMAND.ATA_SMART
                }
            };
            var output = default(SENDCMDOUTPARAMS);
            _ = DeviceIoControl(
                handle,
                DFP_SEND_DRIVE_COMMAND,
                ref parameter,
                SendCommandInParamsSize,
                ref output,
                SendCommandOutParamsSize,
                out _,
                IntPtr.Zero);

            buffer = new SafeNativeMemoryHandle((nuint)BufferLength);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        handle.Dispose();
        buffer.Dispose();
        disposed = true;
    }

    public unsafe bool Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (handle.IsClosed)
        {
            LastUpdate = false;
            return false;
        }

        var span = new Span<byte>(buffer.Pointer, BufferLength);
        span.Clear();

        var parameter = new SENDCMDINPARAMS
        {
            DriveNumber = deviceNumber,
            DriveRegs =
            {
                FeaturesReg = SMART_FEATURES.SMART_READ_DATA,
                CylLowReg = SMART_LBA_MID,
                CylHighReg = SMART_LBA_HI,
                CommandReg = ATA_COMMAND.ATA_SMART
            }
        };
        LastUpdate = DeviceIoControl(
            handle,
            DFP_RECEIVE_DRIVE_DATA,
            ref parameter,
            SendCommandInParamsSize,
            (nint)buffer.Pointer,
            BufferLength,
            out _,
            IntPtr.Zero);
        return LastUpdate;
    }

    public unsafe IReadOnlyList<SmartId> GetSupportedIds()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var list = new List<SmartId>();

        for (var i = 0; i < MAX_DRIVE_ATTRIBUTES; i++)
        {
            var attr = (SMART_ATTRIBUTE*)((byte*)buffer.Pointer + AttributesOffset + (i * AttributesSize));
            if (attr->Id != 0)
            {
                list.Add((SmartId)attr->Id);
            }
        }

        return list;
    }

    public unsafe SmartAttribute? GetAttribute(SmartId id)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var target = (byte)id;
        for (var i = 0; i < MAX_DRIVE_ATTRIBUTES; i++)
        {
            var attr = (SMART_ATTRIBUTE*)((byte*)buffer.Pointer + AttributesOffset + (i * AttributesSize));
            if (attr->Id == target)
            {
                return new SmartAttribute
                {
                    Id = attr->Id,
                    Flags = attr->Flags,
                    CurrentValue = attr->CurrentValue,
                    WorstValue = attr->WorstValue,
                    RawValue = ((ulong)*(ushort*)(attr->RawValue + 4) << 32) + *(uint*)attr->RawValue
                };
            }
        }

        return null;
    }
}
