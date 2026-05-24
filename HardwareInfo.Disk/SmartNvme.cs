namespace HardwareInfo.Disk;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.Helper;
using static HardwareInfo.Disk.NativeMethods;

internal sealed class SmartNvme : ISmartNvme, IDisposable
{
    private static readonly int BufferSize = Marshal.SizeOf<STORAGE_QUERY_BUFFER>();

    private static readonly int QueryBufferOffset;

    private readonly SafeFileHandle handle;

    private unsafe void* buffer;

    public bool LastUpdate { get; private set; }

    public byte CriticalWarning { get; set; }

    public short Temperature { get; set; }

    public byte AvailableSpare { get; set; }

    public byte AvailableSpareThreshold { get; set; }

    public byte PercentageUsed { get; set; }

    public ulong DataUnitRead { get; set; }

    public ulong DataUnitWritten { get; set; }

    public ulong HostReadCommands { get; set; }

    public ulong HostWriteCommands { get; set; }

    public ulong ControllerBusyTime { get; set; }

    public ulong PowerCycles { get; set; }

    public ulong PowerOnHours { get; set; }

    public ulong UnsafeShutdowns { get; set; }

    public ulong MediaErrors { get; set; }

    public ulong ErrorInfoLogEntries { get; set; }

    public uint WarningCompositeTemperatureTime { get; set; }

    public uint CriticalCompositeTemperatureTime { get; set; }

    public short[] TemperatureSensors { get; set; } = new short[8];

#pragma warning disable CA1810
    static unsafe SmartNvme()
    {
        STORAGE_QUERY_BUFFER s = default;
        QueryBufferOffset = (int)(s.Buffer - (byte*)Unsafe.AsPointer(ref s));
    }
#pragma warning restore CA1810

    public unsafe SmartNvme(SafeFileHandle handle)
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

        var query = (STORAGE_QUERY_BUFFER*)buffer;
        query->ProtocolSpecific.ProtocolType = STORAGE_PROTOCOL_TYPE.ProtocolTypeNvme;
        query->ProtocolSpecific.DataType = (uint)STORAGE_PROTOCOL_NVME_DATA_TYPE.NVMeDataTypeLogPage;
        query->ProtocolSpecific.ProtocolDataRequestValue = (uint)NVME_LOG_PAGES.NVME_LOG_PAGE_HEALTH_INFO;
        query->ProtocolSpecific.ProtocolDataOffset = (uint)Marshal.SizeOf<STORAGE_PROTOCOL_SPECIFIC_DATA>();
        query->ProtocolSpecific.ProtocolDataLength = (uint)(BufferSize - QueryBufferOffset);
        query->PropertyId = STORAGE_PROPERTY_ID.StorageAdapterProtocolSpecificProperty;
        query->QueryType = STORAGE_QUERY_TYPE.PropertyStandardQuery;

        if (!DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, (nint)buffer, BufferSize, (nint)buffer, BufferSize, out _, IntPtr.Zero))
        {
            LastUpdate = false;
            return false;
        }

        var log = (NVME_HEALTH_INFO_LOG*)((byte*)buffer + QueryBufferOffset);
        CriticalWarning = log->CriticalWarning;
        Temperature = KelvinToCelsius(*(ushort*)log->CompositeTemp);
        AvailableSpare = log->AvailableSpare;
        AvailableSpareThreshold = log->AvailableSpareThreshold;
        PercentageUsed = log->PercentageUsed;
        DataUnitRead = *(ulong*)log->DataUnitRead;
        DataUnitWritten = *(ulong*)log->DataUnitWritten;
        HostReadCommands = *(ulong*)log->HostReadCommands;
        HostWriteCommands = *(ulong*)log->HostWriteCommands;
        ControllerBusyTime = *(ulong*)log->ControllerBusyTime;
        PowerCycles = *(ulong*)log->PowerCycles;
        PowerOnHours = *(ulong*)log->PowerOnHours;
        UnsafeShutdowns = *(ulong*)log->UnsafeShutdowns;
        MediaErrors = *(ulong*)log->MediaAndDataIntegrityErrors;
        ErrorInfoLogEntries = *(ulong*)log->NumberErrorInformationLogEntries;
        WarningCompositeTemperatureTime = log->WarningCompositeTemperatureTime;
        CriticalCompositeTemperatureTime = log->CriticalCompositeTemperatureTime;
        for (var i = 0; i < TemperatureSensors.Length; i++)
        {
            TemperatureSensors[i] = KelvinToCelsius(log->TemperatureSensor[i]);
        }

        LastUpdate = true;
        return true;
    }
}
