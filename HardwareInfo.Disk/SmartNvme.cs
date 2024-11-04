namespace HardwareInfo.Disk;

using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.Helper;
using static HardwareInfo.Disk.NativeMethods;

internal sealed class SmartNvme : ISmartNvme, IDisposable
{
    private static readonly int QueryBufferOffset =
        Marshal.OffsetOf<STORAGE_QUERY_BUFFER>(nameof(STORAGE_QUERY_BUFFER.Buffer)).ToInt32();

    private readonly SafeFileHandle handle;

    private readonly int length;

    private IntPtr buffer;

    public bool LastUpdate { get; private set; }

    public byte AvailableSpare { get; set; }

    public byte AvailableSpareThreshold { get; set; }

    public ulong ControllerBusyTime { get; set; }

    public uint CriticalCompositeTemperatureTime { get; set; }

    public byte CriticalWarning { get; set; }

    public ulong DataUnitRead { get; set; }

    public ulong DataUnitWritten { get; set; }

    public ulong ErrorInfoLogEntryCount { get; set; }

    public ulong HostReadCommands { get; set; }

    public ulong HostWriteCommands { get; set; }

    public ulong MediaErrors { get; set; }

    public byte PercentageUsed { get; set; }

    public ulong PowerCycle { get; set; }

    public ulong PowerOnHours { get; set; }

    public short Temperature { get; set; }

    public short[] TemperatureSensors { get; set; } = [];

    public ulong UnsafeShutdowns { get; set; }

    public uint WarningCompositeTemperatureTime { get; set; }

    public SmartNvme(SafeFileHandle handle)
    {
        this.handle = handle;
        length = Marshal.SizeOf<STORAGE_QUERY_BUFFER>();
        buffer = Marshal.AllocHGlobal(length);
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

#pragma warning disable CS8500
    public unsafe bool Update()
    {
        if (handle.IsClosed)
        {
            LastUpdate = false;
            return false;
        }

        var span = new Span<byte>(buffer.ToPointer(), length);
        span.Clear();

        var query = Marshal.PtrToStructure<STORAGE_QUERY_BUFFER>(buffer);
        query.ProtocolSpecific.ProtocolType = STORAGE_PROTOCOL_TYPE.ProtocolTypeNvme;
        query.ProtocolSpecific.DataType = (uint)STORAGE_PROTOCOL_NVME_DATA_TYPE.NVMeDataTypeLogPage;
        query.ProtocolSpecific.ProtocolDataRequestValue = (uint)NVME_LOG_PAGES.NVME_LOG_PAGE_HEALTH_INFO;
        query.ProtocolSpecific.ProtocolDataOffset = (uint)Marshal.SizeOf<STORAGE_PROTOCOL_SPECIFIC_DATA>();
        query.ProtocolSpecific.ProtocolDataLength = (uint)query.Buffer.Length;
        query.PropertyId = STORAGE_PROPERTY_ID.StorageAdapterProtocolSpecificProperty;
        query.QueryType = STORAGE_QUERY_TYPE.PropertyStandardQuery;
        Marshal.StructureToPtr(query, buffer, false);

        if (!DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, buffer, length, buffer, length, out _, IntPtr.Zero))
        {
            LastUpdate = false;
            return false;
        }

        var log = Marshal.PtrToStructure<NVME_HEALTH_INFO_LOG>(IntPtr.Add(buffer, QueryBufferOffset));
        CriticalWarning = log.CriticalWarning;
        Temperature = KelvinToCelsius(log.CompositeTemp);
        AvailableSpare = log.AvailableSpare;
        AvailableSpareThreshold = log.AvailableSpareThreshold;
        PercentageUsed = log.PercentageUsed;
        DataUnitRead = BitConverter.ToUInt64(log.DataUnitRead, 0);
        DataUnitWritten = BitConverter.ToUInt64(log.DataUnitWritten, 0);
        HostReadCommands = BitConverter.ToUInt64(log.HostReadCommands, 0);
        HostWriteCommands = BitConverter.ToUInt64(log.HostWriteCommands, 0);
        ControllerBusyTime = BitConverter.ToUInt64(log.ControllerBusyTime, 0);
        PowerCycle = BitConverter.ToUInt64(log.PowerCycles, 0);
        PowerOnHours = BitConverter.ToUInt64(log.PowerOnHours, 0);
        UnsafeShutdowns = BitConverter.ToUInt64(log.UnsafeShutdowns, 0);
        MediaErrors = BitConverter.ToUInt64(log.MediaAndDataIntegrityErrors, 0);
        ErrorInfoLogEntryCount = BitConverter.ToUInt64(log.NumberErrorInformationLogEntries, 0);
        WarningCompositeTemperatureTime = log.WarningCompositeTemperatureTime;
        CriticalCompositeTemperatureTime = log.CriticalCompositeTemperatureTime;
        if (TemperatureSensors.Length != log.TemperatureSensor.Length)
        {
            TemperatureSensors = new short[log.TemperatureSensor.Length];
        }
        for (var i = 0; i < TemperatureSensors.Length; i++)
        {
            TemperatureSensors[i] = KelvinToCelsius(log.TemperatureSensor[i]);
        }

        LastUpdate = true;
        return true;
    }
#pragma warning restore CS8500
}
