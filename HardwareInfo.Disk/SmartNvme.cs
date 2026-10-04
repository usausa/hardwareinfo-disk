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

    private readonly int openError;

    private readonly SafeNativeMemoryHandle buffer;

    private bool disposed;

    public bool LastUpdate { get; private set; }

    public int LastError { get; private set; }

    public byte CriticalWarning { get; private set; }

    public short Temperature { get; private set; }

    public byte AvailableSpare { get; private set; }

    public byte AvailableSpareThreshold { get; private set; }

    public byte PercentageUsed { get; private set; }

    public ulong DataUnitRead { get; private set; }

    public ulong DataUnitWritten { get; private set; }

    public ulong HostReadCommands { get; private set; }

    public ulong HostWriteCommands { get; private set; }

    public ulong ControllerBusyTime { get; private set; }

    public ulong PowerCycles { get; private set; }

    public ulong PowerOnHours { get; private set; }

    public ulong UnsafeShutdowns { get; private set; }

    public ulong MediaErrors { get; private set; }

    public ulong ErrorInfoLogEntries { get; private set; }

    public uint WarningCompositeTemperatureTime { get; private set; }

    public uint CriticalCompositeTemperatureTime { get; private set; }

    public short[] TemperatureSensors { get; } = new short[8];

#pragma warning disable CA1810
    // ReSharper disable once RedundantUnsafeContext
    static unsafe SmartNvme()
    {
        STORAGE_QUERY_BUFFER s = default;
        QueryBufferOffset = (int)(s.Buffer - (byte*)Unsafe.AsPointer(ref s));
    }
#pragma warning restore CA1810

    public SmartNvme(string devicePath)
    {
        handle = OpenDevice(devicePath, FileAccess.ReadWrite);
        openError = handle.IsInvalid ? Marshal.GetLastPInvokeError() : 0;
        try
        {
            buffer = new SafeNativeMemoryHandle((nuint)BufferSize);
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

        if (handle.IsInvalid)
        {
            LastError = openError;
            LastUpdate = false;
            return false;
        }

        var span = new Span<byte>(buffer.Pointer, BufferSize);
        span.Clear();

        var query = (STORAGE_QUERY_BUFFER*)buffer.Pointer;
        query->ProtocolSpecific.ProtocolType = STORAGE_PROTOCOL_TYPE.ProtocolTypeNvme;
        query->ProtocolSpecific.DataType = (uint)STORAGE_PROTOCOL_NVME_DATA_TYPE.NVMeDataTypeLogPage;
        query->ProtocolSpecific.ProtocolDataRequestValue = (uint)NVME_LOG_PAGES.NVME_LOG_PAGE_HEALTH_INFO;
        query->ProtocolSpecific.ProtocolDataOffset = (uint)Marshal.SizeOf<STORAGE_PROTOCOL_SPECIFIC_DATA>();
        query->ProtocolSpecific.ProtocolDataLength = (uint)(BufferSize - QueryBufferOffset);
        query->PropertyId = STORAGE_PROPERTY_ID.StorageAdapterProtocolSpecificProperty;
        query->QueryType = STORAGE_QUERY_TYPE.PropertyStandardQuery;

        if (!DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, (nint)buffer.Pointer, BufferSize, (nint)buffer.Pointer, BufferSize, out _, IntPtr.Zero))
        {
            LastError = Marshal.GetLastPInvokeError();
            LastUpdate = false;
            return false;
        }

        var log = (NVME_HEALTH_INFO_LOG*)((byte*)buffer.Pointer + QueryBufferOffset);
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

        LastError = 0;
        LastUpdate = true;
        return true;
    }
}
