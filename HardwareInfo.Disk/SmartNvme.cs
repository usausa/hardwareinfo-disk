namespace HardwareInfo.Disk;

using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.Helper;
using static HardwareInfo.Disk.NativeMethods;

internal sealed class SmartNvme : ISmartNvme, IDisposable
{
    private static readonly int BufferSize = Marshal.SizeOf<STORAGE_QUERY_BUFFER>();

    private static readonly int QueryBufferOffset = Marshal.OffsetOf<STORAGE_QUERY_BUFFER>(nameof(STORAGE_QUERY_BUFFER.Buffer)).ToInt32();

    private readonly SafeFileHandle handle;

    private IntPtr buffer;

    public bool LastUpdate { get; private set; }

    public byte CriticalWarning { get; set; }

    public short Temperature { get; set; }

    public byte AvailableSpare { get; set; }

    public byte AvailableSpareThreshold { get; set; }

    public byte PercentageUsed { get; set; }

    public ulong DataUnitRead { get; set; }

    public ulong DataUnitWrite { get; set; }

    public ulong HostReadCommand { get; set; }

    public ulong HostWriteCommand { get; set; }

    public ulong ControllerBusyTime { get; set; }

    public ulong PowerCycle { get; set; }

    public ulong PowerOnHour { get; set; }

    public ulong UnsafeShutdown { get; set; }

    public ulong MediaError { get; set; }

    public ulong ErrorInfoLogEntry { get; set; }

    public uint WarningCompositeTemperatureTime { get; set; }

    public uint CriticalCompositeTemperatureTime { get; set; }

    public short[] TemperatureSensors { get; set; } = new short[8];

    public SmartNvme(SafeFileHandle handle)
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

        var query = (STORAGE_QUERY_BUFFER*)buffer;
        query->ProtocolSpecific.ProtocolType = STORAGE_PROTOCOL_TYPE.ProtocolTypeNvme;
        query->ProtocolSpecific.DataType = (uint)STORAGE_PROTOCOL_NVME_DATA_TYPE.NVMeDataTypeLogPage;
        query->ProtocolSpecific.ProtocolDataRequestValue = (uint)NVME_LOG_PAGES.NVME_LOG_PAGE_HEALTH_INFO;
        query->ProtocolSpecific.ProtocolDataOffset = (uint)Marshal.SizeOf<STORAGE_PROTOCOL_SPECIFIC_DATA>();
        query->ProtocolSpecific.ProtocolDataLength = (uint)(BufferSize - QueryBufferOffset);
        query->PropertyId = STORAGE_PROPERTY_ID.StorageAdapterProtocolSpecificProperty;
        query->QueryType = STORAGE_QUERY_TYPE.PropertyStandardQuery;

        if (!DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, buffer, BufferSize, buffer, BufferSize, out _, IntPtr.Zero))
        {
            LastUpdate = false;
            return false;
        }

        var log = (NVME_HEALTH_INFO_LOG*)IntPtr.Add(buffer, QueryBufferOffset);
        CriticalWarning = log->CriticalWarning;
        Temperature = KelvinToCelsius(*(ushort*)log->CompositeTemp);
        AvailableSpare = log->AvailableSpare;
        AvailableSpareThreshold = log->AvailableSpareThreshold;
        PercentageUsed = log->PercentageUsed;
        DataUnitRead = *(ulong*)log->DataUnitRead;
        DataUnitWrite = *(ulong*)log->DataUnitWritten;
        HostReadCommand = *(ulong*)log->HostReadCommands;
        HostWriteCommand = *(ulong*)log->HostWriteCommands;
        ControllerBusyTime = *(ulong*)log->ControllerBusyTime;
        PowerCycle = *(ulong*)log->PowerCycles;
        PowerOnHour = *(ulong*)log->PowerOnHours;
        UnsafeShutdown = *(ulong*)log->UnsafeShutdowns;
        MediaError = *(ulong*)log->MediaAndDataIntegrityErrors;
        ErrorInfoLogEntry = *(ulong*)log->NumberErrorInformationLogEntries;
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
