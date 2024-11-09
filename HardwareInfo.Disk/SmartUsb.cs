namespace HardwareInfo.Disk;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.NativeMethods;

public sealed class SmartUsb : ISmartGeneric, IDisposable
{
    private const int MaxAttributeCount = 30;

    private static readonly short SptSize = (short)Marshal.SizeOf<SCSI_PASS_THROUGH>();
    private static readonly int BufferSize = Marshal.SizeOf<SCSI_PASS_THROUGH_WITH_BUFFERS>();

    private static readonly int SenseOffset = Marshal.OffsetOf<SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(SCSI_PASS_THROUGH_WITH_BUFFERS.Sense)).ToInt32();
    private static readonly int DataOffset = Marshal.OffsetOf<SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(SCSI_PASS_THROUGH_WITH_BUFFERS.Data)).ToInt32();
    private static readonly byte SenseSize = (byte)(DataOffset - SenseOffset);
    private static readonly int DataSize = BufferSize - DataOffset;

    private static readonly int AttributesOffset = DataOffset + 2;
    private static readonly int AttributesSize = Marshal.SizeOf<SMART_ATTRIBUTE>();

    private readonly SafeFileHandle handle;

    private IntPtr buffer;

    public bool LastUpdate { get; private set; }

    public SmartUsb(SafeFileHandle handle)
    {
        this.handle = handle;

        buffer = Marshal.AllocHGlobal(BufferSize);
    }

    public void Dispose()
    {
        handle.Dispose();
        if (buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(buffer);
            buffer = IntPtr.Zero;
        }
    }

    public unsafe bool Update()
    {
        if (handle.IsClosed)
        {
            LastUpdate = false;
            return false;
        }

        var span = new Span<byte>(buffer.ToPointer(), BufferSize);
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
            buffer,
            BufferSize,
            buffer,
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
            var attr = (SMART_ATTRIBUTE*)IntPtr.Add(buffer, AttributesOffset + (i * AttributesSize));
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
            var attr = (SMART_ATTRIBUTE*)(buffer + AttributesOffset + (i * AttributesSize));
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
