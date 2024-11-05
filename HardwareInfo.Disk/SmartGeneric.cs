namespace HardwareInfo.Disk;

using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.NativeMethods;

internal sealed class SmartGeneric : ISmartGeneric, IDisposable
{
    private static readonly int SendCommandInParamsSize = Marshal.SizeOf<SENDCMDINPARAMS>();
    private static readonly int SendCommandOutParamsSize = Marshal.SizeOf<SENDCMDOUTPARAMS>();

    private static readonly int BufferSize = Marshal.SizeOf<ATTRIBUTECMDOUTPARAMS>();

    private static readonly int AttributesSize = Marshal.SizeOf<SMART_ATTRIBUTE>();
    private static readonly int AttributesOffset = Marshal.OffsetOf<ATTRIBUTECMDOUTPARAMS>(nameof(ATTRIBUTECMDOUTPARAMS.Attributes)).ToInt32();

    private readonly SafeFileHandle handle;

    private readonly byte deviceNumber;

    private IntPtr buffer;

    public bool LastUpdate { get; private set; }

    public SmartGeneric(SafeFileHandle handle, int deviceNumber)
    {
        this.handle = handle;
        this.deviceNumber = (byte)deviceNumber;

        var parameter = new SENDCMDINPARAMS
        {
            bDriveNumber = this.deviceNumber,
            irDriveRegs =
            {
                bFeaturesReg = SMART_FEATURES.ENABLE_SMART,
                bCylLowReg = SMART_LBA_MID,
                bCylHighReg = SMART_LBA_HI,
                bCommandReg = ATA_COMMAND.ATA_SMART
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

        var parameter = new SENDCMDINPARAMS
        {
            bDriveNumber = deviceNumber,
            irDriveRegs =
            {
                bFeaturesReg = SMART_FEATURES.SMART_READ_DATA,
                bCylLowReg = SMART_LBA_MID,
                bCylHighReg = SMART_LBA_HI,
                bCommandReg = ATA_COMMAND.ATA_SMART
            }
        };
        LastUpdate = DeviceIoControl(
            handle,
            DFP_RECEIVE_DRIVE_DATA,
            ref parameter,
            SendCommandInParamsSize,
            buffer,
            BufferSize,
            out _,
            IntPtr.Zero);
        return LastUpdate;
    }

    public unsafe IReadOnlyList<SmartId> GetSupportedIds()
    {
        var list = new List<SmartId>();

        for (var i = 0; i < MAX_DRIVE_ATTRIBUTES; i++)
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
        for (var i = 0; i < MAX_DRIVE_ATTRIBUTES; i++)
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
