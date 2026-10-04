namespace HardwareInfo.Disk;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.Helper;
using static HardwareInfo.Disk.NativeMethods;

internal sealed class SmartUsb : ISmartGeneric, IDisposable
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
    private static readonly int ThresholdsSize = Marshal.SizeOf<SMART_THRESHOLD>();

    private readonly SafeFileHandle handle;

    private readonly int openError;

    private readonly SafeNativeMemoryHandle buffer;

    private readonly SafeNativeMemoryHandle thresholds;

    private bool thresholdsLoaded;

    private bool disposed;

    public bool LastUpdate { get; private set; }

    public int LastError { get; private set; }

    public SmartAssessment Assessment { get; private set; }

#pragma warning disable CA1810
    // ReSharper disable once RedundantUnsafeContext
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

    public SmartUsb(string devicePath)
    {
        handle = OpenDevice(devicePath, FileAccess.ReadWrite);
        openError = handle.IsInvalid ? Marshal.GetLastPInvokeError() : 0;
        try
        {
            buffer = new SafeNativeMemoryHandle((nuint)BufferSize);
            thresholds = new SafeNativeMemoryHandle((nuint)BufferSize);
        }
        catch
        {
            buffer?.Dispose();
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
        thresholds.Dispose();
        disposed = true;
    }

    public bool Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (handle.IsInvalid)
        {
            LastError = openError;
            LastUpdate = false;
            return false;
        }

        if (!ReadData(buffer, READ_ATTRIBUTES))
        {
            LastUpdate = false;
            return false;
        }

        if (!thresholdsLoaded)
        {
            thresholdsLoaded = ReadData(thresholds, READ_THRESHOLDS);
        }

        Assessment = ReadAssessment();

        LastError = 0;
        LastUpdate = true;
        return true;
    }

    private unsafe bool ReadData(SafeNativeMemoryHandle data, byte feature)
    {
        var span = new Span<byte>(data.Pointer, BufferSize);
        span.Clear();

        var swb = (SCSI_PASS_THROUGH_WITH_BUFFERS*)data.Pointer;
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
        swb->Spt.Cdb[3] = feature;
        swb->Spt.Cdb[4] = 0x01;
        swb->Spt.Cdb[5] = 0x01;
        swb->Spt.Cdb[6] = SMART_LBA_MID;
        swb->Spt.Cdb[7] = SMART_LBA_HI;
        swb->Spt.Cdb[8] = 0x00;
        swb->Spt.Cdb[9] = SMART_CMD;

        var ret = DeviceIoControl(
            handle,
            IOCTL_SCSI_PASS_THROUGH,
            (nint)data.Pointer,
            BufferSize,
            (nint)data.Pointer,
            BufferSize,
            out var returnedBytes,
            IntPtr.Zero);
        if (!ret)
        {
            LastError = Marshal.GetLastPInvokeError();
            return false;
        }

        if (returnedBytes <= DataOffset)
        {
            LastError = ERROR_IO_DEVICE;
            return false;
        }

        return true;
    }

    private unsafe SmartAssessment ReadAssessment()
    {
        var swb = default(SCSI_PASS_THROUGH_WITH_BUFFERS);
        swb.Spt.Length = SptSize;
        swb.Spt.CdbLength = 12;
        swb.Spt.DataIn = SCSI_IOCTL_DATA_UNSPECIFIED;
        swb.Spt.SenseInfoLength = SenseSize;
        swb.Spt.SenseInfoOffset = SenseOffset;
        swb.Spt.DataTransferLength = 0;
        swb.Spt.TimeOutValue = 5;

        swb.Spt.Cdb[0] = 0xA1;      // ATA PASS-THROUGH(12)
        swb.Spt.Cdb[1] = 0x06;      // protocol = 3 (Non-data)
        swb.Spt.Cdb[2] = 0x20;      // ck_cond=1
        swb.Spt.Cdb[3] = RETURN_SMART_STATUS;
        swb.Spt.Cdb[6] = SMART_LBA_MID;
        swb.Spt.Cdb[7] = SMART_LBA_HI;
        swb.Spt.Cdb[9] = SMART_CMD;

        if (!DeviceIoControl(handle, IOCTL_SCSI_PASS_THROUGH, (nint)(&swb), BufferSize, (nint)(&swb), BufferSize, out _, IntPtr.Zero))
        {
            return SmartAssessment.Unknown;
        }

        return ParseAssessment(new ReadOnlySpan<byte>(swb.Sense, Math.Min((int)swb.Spt.SenseInfoLength, SenseSize)));
    }

    private static SmartAssessment ParseAssessment(ReadOnlySpan<byte> sense)
    {
        if (sense.Length < 8)
        {
            return SmartAssessment.Unknown;
        }

        var responseCode = sense[0] & 0x7F;
        if (responseCode is 0x72 or 0x73)
        {
            // Descriptor format (ATA Status Return descriptor)
            var end = Math.Min(sense.Length, 8 + sense[7]);
            var offset = 8;
            while (offset + 1 < end)
            {
                if ((sense[offset] == 0x09) && (offset + 14 <= end))
                {
                    return ToAssessment(sense[offset + 9], sense[offset + 11]);
                }

                offset += sense[offset + 1] + 2;
            }

            return SmartAssessment.Unknown;
        }

        if ((responseCode is 0x70 or 0x71) && (sense.Length >= 12))
        {
            // Fixed format (LBA 15:8 / 23:16 in command-specific information)
            return ToAssessment(sense[10], sense[11]);
        }

        return SmartAssessment.Unknown;
    }

    private static SmartAssessment ToAssessment(byte lbaMid, byte lbaHigh) =>
        (lbaMid, lbaHigh) switch
        {
            (SMART_LBA_MID, SMART_LBA_HI) => SmartAssessment.Passed,
            (SMART_LBA_MID_EXCEEDED, SMART_LBA_HI_EXCEEDED) => SmartAssessment.Failed,
            _ => SmartAssessment.Unknown
        };

    public unsafe IReadOnlyList<SmartId> GetSupportedIds()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var list = new List<SmartId>();

        for (var i = 0; i < MaxAttributeCount; i++)
        {
            var attr = (SMART_ATTRIBUTE*)((byte*)buffer.Pointer + AttributesOffset + (i * AttributesSize));
            if ((attr->Id != 0) && (attr->Id != 0xFF))
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
        for (var i = 0; i < MaxAttributeCount; i++)
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
                    Threshold = FindThreshold(target),
                    RawValue = ((ulong)*(ushort*)(attr->RawValue + 4) << 32) + *(uint*)attr->RawValue
                };
            }
        }

        return null;
    }

    private unsafe byte FindThreshold(byte id)
    {
        if (!thresholdsLoaded)
        {
            return 0;
        }

        for (var i = 0; i < MaxAttributeCount; i++)
        {
            var threshold = (SMART_THRESHOLD*)((byte*)thresholds.Pointer + AttributesOffset + (i * ThresholdsSize));
            if (threshold->Id == id)
            {
                return threshold->Threshold;
            }
        }

        return 0;
    }
}
