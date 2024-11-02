namespace HardwareInfo.Smart;

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;

public static class StorageInformation
{
    public static IStorage[] GetInformation()
    {
        var storages = new List<IStorage>();

        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
        foreach (var drive in searcher.Get())
        {
            var storage = new AbstractStorage();
            storage.DeviceId = (string)drive.Properties["DeviceID"].Value;
            storage.Index = Convert.ToInt32(drive.Properties["Index"].Value, CultureInfo.InvariantCulture);
            storage.Size = Convert.ToUInt64(drive.Properties["Size"].Value, CultureInfo.InvariantCulture);
            storage.ScsiPort = Convert.ToInt32(drive.Properties["SCSIPort"].Value, CultureInfo.InvariantCulture);
            storage.Size = Convert.ToUInt64(drive.Properties["Size"].Value, CultureInfo.InvariantCulture);
            storage.PnpDeviceId = (string)drive.Properties["PNPDeviceID"].Value;
            storage.Status = (string)drive.Properties["Status"].Value;
            storage.Model = (string)drive.Properties["Model"].Value;
            storage.SerialNumber = (string)drive.Properties["SerialNumber"].Value;
            storage.FirmwareRevision = (string)drive.Properties["FirmwareRevision"].Value;

            var info = GetStorageInfo(storage.DeviceId, storage.Index);
            if (info is null)
            {
                continue;
            }

            // TODO Ata
            if (info.BusType != Kernel32.STORAGE_BUS_TYPE.BusTypeNvme)
            {
                continue;
            }

            using var handle = Kernel32.OpenDevice(storage.DeviceId);
            if (handle is null)
            {
                continue;
            }

            // Controller
            var data = new Kernel32.NVME_IDENTIFY_CONTROLLER_DATA();
            if (!IdentifyController(handle, out data))
            {
                continue;
            }

            var nvmeInfo = new NVMeInfo(info.Index, data);

            var log = new Kernel32.NVME_HEALTH_INFO_LOG();
            if (!HealthInfoLog(handle, out log))
            {
                continue;
            }

            var health = new NVMeHealthInfo(log);

            // TODO
            storages.Add(storage);

            Console.WriteLine(storage.Model);
            Console.WriteLine($"  Size            : {storage.Size}");
            Console.WriteLine($"  Status          : {storage.Status}");

            Console.WriteLine($"  DataUnitRead    : {health.DataUnitRead}");
            Console.WriteLine($"  DataUnitWritten : {health.DataUnitWritten}");
            Console.WriteLine($"  AvailableSpare  : {health.AvailableSpare}");
            Console.WriteLine($"  PercentageUsed  : {health.PercentageUsed}");
            Console.WriteLine($"  PowerCycle      : {health.PowerCycle}");
            Console.WriteLine($"  PowerOnHours    : {health.PowerOnHours}");
            Console.WriteLine($"  Temperature     : {health.Temperature}");
            Console.WriteLine($"  UnsafeShutdowns : {health.UnsafeShutdowns}");
        }

        return storages.ToArray();
    }

    private static StorageInfo? GetStorageInfo(string deviceId, int driveIndex)
    {
        using var handle = Kernel32.OpenDevice(deviceId);
        if (handle?.IsInvalid != false)
        {
            return null;
        }

        var query = new Kernel32.STORAGE_PROPERTY_QUERY
        {
            PropertyId = Kernel32.STORAGE_PROPERTY_ID.StorageDeviceProperty,
            QueryType = Kernel32.STORAGE_QUERY_TYPE.PropertyStandardQuery
        };

        if (!Kernel32.DeviceIoControl(
                handle,
                Kernel32.IOCTL.IOCTL_STORAGE_QUERY_PROPERTY,
                ref query,
                Marshal.SizeOf(query),
                out var header,
                Marshal.SizeOf<Kernel32.STORAGE_DEVICE_DESCRIPTOR_HEADER>(),
                out _,
                IntPtr.Zero))
        {
            return null;
        }

        var descriptorPtr = Marshal.AllocHGlobal((int)header.Size);

        try
        {
            return Kernel32.DeviceIoControl(
                handle,
                Kernel32.IOCTL.IOCTL_STORAGE_QUERY_PROPERTY,
                ref query,
                Marshal.SizeOf(query),
                descriptorPtr,
                header.Size,
                out _,
                IntPtr.Zero)
                ? new StorageInfo(driveIndex, descriptorPtr)
                : null;
        }
        finally
        {
            Marshal.FreeHGlobal(descriptorPtr);
        }
    }

    internal static bool IdentifyController(SafeHandle hDevice, out Kernel32.NVME_IDENTIFY_CONTROLLER_DATA data)
    {
        data = Kernel32.CreateStruct<Kernel32.NVME_IDENTIFY_CONTROLLER_DATA>();
        if (hDevice?.IsInvalid != false)
            return false;

        bool result = false;
        Kernel32.STORAGE_QUERY_BUFFER nptwb = Kernel32.CreateStruct<Kernel32.STORAGE_QUERY_BUFFER>();
        nptwb.ProtocolSpecific.ProtocolType = Kernel32.STORAGE_PROTOCOL_TYPE.ProtocolTypeNvme;
        nptwb.ProtocolSpecific.DataType = (uint)Kernel32.STORAGE_PROTOCOL_NVME_DATA_TYPE.NVMeDataTypeIdentify;
        nptwb.ProtocolSpecific.ProtocolDataRequestValue = (uint)Kernel32.STORAGE_PROTOCOL_NVME_PROTOCOL_DATA_REQUEST_VALUE.NVMeIdentifyCnsController;
        nptwb.ProtocolSpecific.ProtocolDataOffset = (uint)Marshal.SizeOf<Kernel32.STORAGE_PROTOCOL_SPECIFIC_DATA>();
        nptwb.ProtocolSpecific.ProtocolDataLength = (uint)nptwb.Buffer.Length;
        nptwb.PropertyId = Kernel32.STORAGE_PROPERTY_ID.StorageAdapterProtocolSpecificProperty;
        nptwb.QueryType = Kernel32.STORAGE_QUERY_TYPE.PropertyStandardQuery;

        int length = Marshal.SizeOf<Kernel32.STORAGE_QUERY_BUFFER>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(nptwb, buffer, false);
        bool validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_STORAGE_QUERY_PROPERTY, buffer, length, buffer, length, out _, IntPtr.Zero);
        if (validTransfer)
        {
            //map NVME_IDENTIFY_CONTROLLER_DATA to nptwb.Buffer
            IntPtr offset = Marshal.OffsetOf<Kernel32.STORAGE_QUERY_BUFFER>(nameof(Kernel32.STORAGE_QUERY_BUFFER.Buffer));
            var newPtr = IntPtr.Add(buffer, offset.ToInt32());
            data = Marshal.PtrToStructure<Kernel32.NVME_IDENTIFY_CONTROLLER_DATA>(newPtr);
            Marshal.FreeHGlobal(buffer);
            result = true;
        }
        else
        {
            Marshal.FreeHGlobal(buffer);
        }

        return result;
    }

    internal static bool HealthInfoLog(SafeHandle hDevice, out Kernel32.NVME_HEALTH_INFO_LOG data)
    {
        data = Kernel32.CreateStruct<Kernel32.NVME_HEALTH_INFO_LOG>();
        if (hDevice.IsInvalid != false)
        {
            return false;
        }

        bool result = false;
        Kernel32.STORAGE_QUERY_BUFFER nptwb = Kernel32.CreateStruct<Kernel32.STORAGE_QUERY_BUFFER>();
        nptwb.ProtocolSpecific.ProtocolType = Kernel32.STORAGE_PROTOCOL_TYPE.ProtocolTypeNvme;
        nptwb.ProtocolSpecific.DataType = (uint)Kernel32.STORAGE_PROTOCOL_NVME_DATA_TYPE.NVMeDataTypeLogPage;
        nptwb.ProtocolSpecific.ProtocolDataRequestValue = (uint)Kernel32.NVME_LOG_PAGES.NVME_LOG_PAGE_HEALTH_INFO;
        nptwb.ProtocolSpecific.ProtocolDataOffset = (uint)Marshal.SizeOf<Kernel32.STORAGE_PROTOCOL_SPECIFIC_DATA>();
        nptwb.ProtocolSpecific.ProtocolDataLength = (uint)nptwb.Buffer.Length;
        nptwb.PropertyId = Kernel32.STORAGE_PROPERTY_ID.StorageAdapterProtocolSpecificProperty;
        nptwb.QueryType = Kernel32.STORAGE_QUERY_TYPE.PropertyStandardQuery;

        int length = Marshal.SizeOf<Kernel32.STORAGE_QUERY_BUFFER>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(nptwb, buffer, false);
        bool validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_STORAGE_QUERY_PROPERTY, buffer, length, buffer, length, out _, IntPtr.Zero);
        if (validTransfer)
        {
            //map NVME_HEALTH_INFO_LOG to nptwb.Buffer
            IntPtr offset = Marshal.OffsetOf<Kernel32.STORAGE_QUERY_BUFFER>(nameof(Kernel32.STORAGE_QUERY_BUFFER.Buffer));
            var newPtr = IntPtr.Add(buffer, offset.ToInt32());
            data = Marshal.PtrToStructure<Kernel32.NVME_HEALTH_INFO_LOG>(newPtr);
            Marshal.FreeHGlobal(buffer);
            result = true;
        }
        else
        {
            Marshal.FreeHGlobal(buffer);
        }

        return result;
    }
}

public interface IStorage
{
    string DeviceId { get; }
}

// TODO abstract
internal class AbstractStorage : IStorage
{
    public string DeviceId { get; set; } = default!;

    public int Index { get; set; }

    public ulong Size { get; set; }

    public int ScsiPort { get; set; }

    public string PnpDeviceId { get; set; } = default!;

    public string Status { get; set; } = default!;

    public string Model { get; set; } = default!;

    public string SerialNumber { get; set; } = default!;

    public string FirmwareRevision { get; set; } = default!;
}

internal class StorageInfo
{
    public Kernel32.STORAGE_BUS_TYPE BusType { get; protected set; }

    public SafeFileHandle Handle { get; set; } = default!;

    public int Index { get; protected set; }

    public string Product { get; protected set; }

    public byte[] RawData { get; protected set; }

    public bool Removable { get; protected set; }

    public string Revision { get; protected set; }

    public string Scsi { get; set; } = default!;

    public string Serial { get; protected set; }

    public string Vendor { get; protected set; }

    public StorageInfo(int index, IntPtr descriptorPtr)
    {
        var descriptor = Marshal.PtrToStructure<Kernel32.STORAGE_DEVICE_DESCRIPTOR>(descriptorPtr);
        Index = index;
        Vendor = GetString(descriptorPtr, descriptor.VendorIdOffset, descriptor.Size);
        Product = GetString(descriptorPtr, descriptor.ProductIdOffset, descriptor.Size);
        Revision = GetString(descriptorPtr, descriptor.ProductRevisionOffset, descriptor.Size);
        Serial = GetString(descriptorPtr, descriptor.SerialNumberOffset, descriptor.Size);
        BusType = descriptor.BusType;
        Removable = descriptor.RemovableMedia;
        RawData = new byte[descriptor.Size];
        Marshal.Copy(descriptorPtr, RawData, 0, RawData.Length);
    }

    private static string GetString(IntPtr descriptorPtr, uint offset, uint size)
    {
        return offset > 0 && offset < size
            ? Marshal.PtrToStringAnsi(IntPtr.Add(descriptorPtr, (int)offset))?.Trim() ?? string.Empty
            : string.Empty;
    }
}

public class NVMeInfo
{
    public ushort ControllerId { get; protected set; }

    public byte[] IEEE { get; protected set; }

    public int Index { get; protected set; }

    public string Model { get; protected set; }

    public uint NumberNamespaces { get; protected set; }

    public string Revision { get; protected set; }

    public string Serial { get; protected set; }

    public ushort SSVID { get; protected set; }

    public ulong TotalCapacity { get; protected set; }

    public ulong UnallocatedCapacity { get; protected set; }

    public ushort VID { get; protected set; }

    internal NVMeInfo(int index, Kernel32.NVME_IDENTIFY_CONTROLLER_DATA data)
    {
        Index = index;
        VID = data.VID;
        SSVID = data.SSVID;
        Serial = GetString(data.SN);
        Model = GetString(data.MN);
        Revision = GetString(data.FR);
        IEEE = data.IEEE;
        TotalCapacity = BitConverter.ToUInt64(data.TNVMCAP, 0); // 128bit little endian
        UnallocatedCapacity = BitConverter.ToUInt64(data.UNVMCAP, 0);
        ControllerId = data.CNTLID;
        NumberNamespaces = data.NN;
    }

    private static string GetString(byte[] s)
    {
        return Encoding.ASCII.GetString(s).Trim('\t', '\n', '\r', ' ', '\0');
    }
}

public class NVMeHealthInfo
{
    public byte AvailableSpare { get; protected set; }

    public byte AvailableSpareThreshold { get; protected set; }

    public ulong ControllerBusyTime { get; protected set; }

    public uint CriticalCompositeTemperatureTime { get; protected set; }

    public Kernel32.NVME_CRITICAL_WARNING CriticalWarning { get; protected set; }

    public ulong DataUnitRead { get; protected set; }

    public ulong DataUnitWritten { get; protected set; }

    public ulong ErrorInfoLogEntryCount { get; protected set; }

    public ulong HostReadCommands { get; protected set; }

    public ulong HostWriteCommands { get; protected set; }

    public ulong MediaErrors { get; protected set; }

    public byte PercentageUsed { get; protected set; }

    public ulong PowerCycle { get; protected set; }

    public ulong PowerOnHours { get; protected set; }

    public short Temperature { get; protected set; }

    public short[] TemperatureSensors { get; protected set; }

    public ulong UnsafeShutdowns { get; protected set; }

    public uint WarningCompositeTemperatureTime { get; protected set; }

    internal NVMeHealthInfo(Kernel32.NVME_HEALTH_INFO_LOG log)
    {
        CriticalWarning = (Kernel32.NVME_CRITICAL_WARNING)log.CriticalWarning;
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

        TemperatureSensors = new short[log.TemperatureSensor.Length];
        for (int i = 0; i < TemperatureSensors.Length; i++)
            TemperatureSensors[i] = KelvinToCelsius(log.TemperatureSensor[i]);
    }

    private static short KelvinToCelsius(ushort k)
    {
        return (short)(k > 0 ? k - 273 : short.MinValue);
    }

    private static short KelvinToCelsius(byte[] k)
    {
        return KelvinToCelsius(BitConverter.ToUInt16(k, 0));
    }
}
