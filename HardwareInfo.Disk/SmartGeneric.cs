namespace HardwareInfo.Disk;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.Helper;
using static HardwareInfo.Disk.NativeMethods;

internal sealed class SmartGeneric : ISmartGeneric, IDisposable
{
    private const int MaxAttributeCount = 30;

    private static readonly int SendCommandInParamsSize = Marshal.SizeOf<SENDCMDINPARAMS>();
    private static readonly int SendCommandOutParamsSize = Marshal.SizeOf<SENDCMDOUTPARAMS>();
    private static readonly int StatusCommandOutParamsSize = Marshal.SizeOf<STATUSCMDOUTPARAMS>();

    private static readonly int BufferLength = Marshal.SizeOf<ATTRIBUTECMDOUTPARAMS>();
    private static readonly int ThresholdsLength = Marshal.SizeOf<THRESHOLDCMDOUTPARAMS>();

    private static readonly int AttributesSize = Marshal.SizeOf<SMART_ATTRIBUTE>();
    private static readonly int ThresholdsSize = Marshal.SizeOf<SMART_THRESHOLD>();

    private static readonly int AttributesOffset;
    private static readonly int ThresholdsOffset;

    private readonly SafeFileHandle handle;

    private readonly int openError;

    private readonly byte deviceNumber;

    private readonly SafeNativeMemoryHandle buffer;

    private readonly SafeNativeMemoryHandle thresholds;

    private bool thresholdsLoaded;

    private bool disposed;

    public bool LastUpdate { get; private set; }

    public int LastError { get; private set; }

    public SmartAssessment Assessment { get; private set; }

#pragma warning disable CA1810
    // ReSharper disable once RedundantUnsafeContext
    static unsafe SmartGeneric()
    {
        ATTRIBUTECMDOUTPARAMS s = default;
        AttributesOffset = (int)(s.Attributes - (byte*)Unsafe.AsPointer(ref s));
        THRESHOLDCMDOUTPARAMS t = default;
        ThresholdsOffset = (int)(t.Thresholds - (byte*)Unsafe.AsPointer(ref t));
    }
#pragma warning restore CA1810

    public SmartGeneric(string devicePath, byte deviceNumber)
    {
        handle = OpenDevice(devicePath, FileAccess.ReadWrite);
        openError = handle.IsInvalid ? Marshal.GetLastPInvokeError() : 0;
        this.deviceNumber = deviceNumber;
        try
        {
            if (!handle.IsInvalid)
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
            }

            buffer = new SafeNativeMemoryHandle((nuint)BufferLength);
            thresholds = new SafeNativeMemoryHandle((nuint)ThresholdsLength);
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

        if (!ReadData(buffer, BufferLength, SMART_FEATURES.SMART_READ_DATA))
        {
            LastUpdate = false;
            return false;
        }

        if (!thresholdsLoaded)
        {
            thresholdsLoaded = ReadData(thresholds, ThresholdsLength, SMART_FEATURES.READ_THRESHOLDS);
        }

        Assessment = ReadAssessment();

        LastError = 0;
        LastUpdate = true;
        return true;
    }

    // ReSharper disable once RedundantUnsafeContext
    private unsafe bool ReadData(SafeNativeMemoryHandle data, int length, SMART_FEATURES feature)
    {
        var span = new Span<byte>(data.Pointer, length);
        span.Clear();

        var parameter = new SENDCMDINPARAMS
        {
            DriveNumber = deviceNumber,
            DriveRegs =
            {
                FeaturesReg = feature,
                CylLowReg = SMART_LBA_MID,
                CylHighReg = SMART_LBA_HI,
                CommandReg = ATA_COMMAND.ATA_SMART
            }
        };
        if (!DeviceIoControl(handle, DFP_RECEIVE_DRIVE_DATA, ref parameter, SendCommandInParamsSize, (nint)data.Pointer, length, out _, IntPtr.Zero))
        {
            LastError = Marshal.GetLastPInvokeError();
            return false;
        }

        return true;
    }

    private SmartAssessment ReadAssessment()
    {
        var parameter = new SENDCMDINPARAMS
        {
            DriveNumber = deviceNumber,
            DriveRegs =
            {
                FeaturesReg = SMART_FEATURES.RETURN_SMART_STATUS,
                CylLowReg = SMART_LBA_MID,
                CylHighReg = SMART_LBA_HI,
                CommandReg = ATA_COMMAND.ATA_SMART
            }
        };
        var output = default(STATUSCMDOUTPARAMS);
        if (!DeviceIoControl(handle, DFP_SEND_DRIVE_COMMAND, ref parameter, SendCommandInParamsSize, ref output, StatusCommandOutParamsSize, out _, IntPtr.Zero))
        {
            return SmartAssessment.Unknown;
        }

        return (output.DriveRegs.CylLowReg, output.DriveRegs.CylHighReg) switch
        {
            (SMART_LBA_MID, SMART_LBA_HI) => SmartAssessment.Passed,
            (SMART_LBA_MID_EXCEEDED, SMART_LBA_HI_EXCEEDED) => SmartAssessment.Failed,
            _ => SmartAssessment.Unknown
        };
    }

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
            var threshold = (SMART_THRESHOLD*)((byte*)thresholds.Pointer + ThresholdsOffset + (i * ThresholdsSize));
            if (threshold->Id == id)
            {
                return threshold->Threshold;
            }
        }

        return 0;
    }
}
