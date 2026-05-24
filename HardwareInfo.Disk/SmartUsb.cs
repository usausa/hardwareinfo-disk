namespace HardwareInfo.Disk;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.NativeMethods;

public sealed class SmartUsb : ISmartGeneric, IDisposable
{
    private const int MaxAttributeCount = 30;

    private static readonly short SptSize = (short)Marshal.SizeOf<SCSI_PASS_THROUGH>();
    private static readonly int BufferSize = Marshal.SizeOf<SCSI_PASS_THROUGH_WITH_BUFFERS>();

    private static readonly int SenseOffset;
    private static readonly int DataOffset;
    private static readonly byte SenseSize;
    private static readonly int DataSize;
    private static readonly int AttributesOffset;

    private static readonly int AttributesSize = Marshal.SizeOf<SMART_ATTRIBUTE>();

    private readonly SafeFileHandle handle;

    private unsafe void* buffer;

    public bool LastUpdate { get; private set; }

#pragma warning disable CA1810
    static unsafe SmartUsb()
    {
        SCSI_PASS_THROUGH_WITH_BUFFERS s = default;
        SenseOffset = (int)(s.Sense - (byte*)Unsafe.AsPointer(ref s));
        DataOffset = (int)(s.Data - (byte*)Unsafe.AsPointer(ref s));
        SenseSize = (byte)(DataOffset - SenseOffset);
        DataSize = Marshal.SizeOf<SCSI_PASS_THROUGH_WITH_BUFFERS>() - DataOffset;
        AttributesOffset = DataOffset + 2;
    }
#pragma warning restore CA1810

    public unsafe SmartUsb(SafeFileHandle handle)
    {
        this.handle = handle;
        buffer = NativeMemory.Alloc((nuint)BufferSize);
    }

    public unsafe void Dispose()
    {
        handle.Dispose();
        if (buffer != null)
        {
            NativeMemory.Free(buffer);
            buffer = null;
        }
    }

    public unsafe bool Update()
    {
        if (handle.IsClosed)
        {
            LastUpdate = false;
            return false;
        }

        var span = new Span<byte>(buffer, BufferSize);
        span.Clear();

        var swb = (SCSI_PASS_THROUGH_WITH_BUFFERS*)buffer;
        swb->Spt.Length = SptSize;
        swb->Spt.CdbLength = 12;
        swb->Spt.DataIn = SCSI_IOCTL_DATA_IN;
        swb->Spt.SenseInfoLength = SenseSize;
        swb->Spt.SenseInfoOffset = SenseOffset;
        swb->Spt.DataTransferLength = DataSize;
        swb->Spt.DataBufferOffset = DataOffset;
        swb->Spt.TimeOutValue = 5;

        swb->Spt.Cdb[0] = 0xA1;
        swb->Spt.Cdb[1] = 0x08;
        swb->Spt.Cdb[2] = 0x0E;
        swb->Spt.Cdb[3] = READ_ATTRIBUTES;
        swb->Spt.Cdb[4] = 0x01;
        swb->Spt.Cdb[5] = 0x01;
        swb->Spt.Cdb[6] = SMART_LBA_MID;
        swb->Spt.Cdb[7] = SMART_LBA_HI;
        swb->Spt.Cdb[8] = 0x00;
        swb->Spt.Cdb[9] = SMART_CMD;

        var ret = DeviceIoControl(
            handle,
            IOCTL_SCSI_PASS_THROUGH,
            (nint)buffer,
            BufferSize,
            (nint)buffer,
            BufferSize,
            out var returnedBytes,
            IntPtr.Zero);
        LastUpdate = ret && returnedBytes > DataOffset;

        return LastUpdate;
    }

    public unsafe IReadOnlyList<SmartId> GetSupportedIds()
    {
        var list = new List<SmartId>();

        for (var i = 0; i < MaxAttributeCount; i++)
        {
            var attr = (SMART_ATTRIBUTE*)((byte*)buffer + AttributesOffset + (i * AttributesSize));
            if (attr->Id != 0)
            {
                list.Add((SmartId)attr->Id);
            }
        }

        return list;
    }

    public unsafe SmartAttribute? GetAttribute(SmartId id)
    {
        var target = (byte)id;
        for (var i = 0; i < MaxAttributeCount; i++)
        {
            var attr = (SMART_ATTRIBUTE*)((byte*)buffer + AttributesOffset + (i * AttributesSize));
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
