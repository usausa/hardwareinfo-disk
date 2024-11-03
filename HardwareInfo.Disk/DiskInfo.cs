namespace HardwareInfo.Disk;

using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.NativeMethods;

[SupportedOSPlatform("windows")]
public static class DiskInfo
{
    public static IDiskInfo[] GetInformation()
    {
        var list = new List<IDiskInfo>();

        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
        foreach (var disk in searcher.Get())
        {
            var info = new DiscInfoGeneric
            {
                DeviceId = (string)disk.Properties["DeviceID"].Value,
                Index = Convert.ToInt32(disk.Properties["Index"].Value, CultureInfo.InvariantCulture),
                Size = Convert.ToUInt64(disk.Properties["Size"].Value, CultureInfo.InvariantCulture),
                PnpDeviceId = (string)disk.Properties["PNPDeviceID"].Value,
                Status = (string)disk.Properties["Status"].Value,
                Model = (string)disk.Properties["Model"].Value,
                SerialNumber = (string)disk.Properties["SerialNumber"].Value,
                FirmwareRevision = (string)disk.Properties["FirmwareRevision"].Value
            };
            list.Add(info);

            // Get descriptor
            var descriptor = GetStorageDescriptor(info.DeviceId);
            if (descriptor is null)
            {
                info.DiskType = DiskType.Unsupported;
                info.Smart = SmartUnsupported.Default;
                continue;
            }

            info.Removable = descriptor.Removable;

            // NVMe
            if (descriptor.BusType == STORAGE_BUS_TYPE.BusTypeNvme)
            {
                info.DiskType = DiskType.Nvme;
                info.Smart = new SmartNvme(OpenDevice(info.DeviceId));
                info.Smart.Update();
                continue;
            }

            // TODO ATA

            info.DiskType = DiskType.Unsupported;
            info.Smart = SmartUnsupported.Default;
        }

        return [.. list];
    }

    //------------------------------------------------------------------------
    // Helper
    //------------------------------------------------------------------------

    private static SafeFileHandle OpenDevice(string devicePath) =>
        CreateFile(devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);

    private sealed record StorageDescriptor(STORAGE_BUS_TYPE BusType, bool Removable);

    private static StorageDescriptor? GetStorageDescriptor(string devicePath)
    {
        using var handle = OpenDevice(devicePath);
        if (handle.IsInvalid)
        {
            return null;
        }

        var query = new STORAGE_PROPERTY_QUERY
        {
            PropertyId = STORAGE_PROPERTY_ID.StorageDeviceProperty,
            QueryType = STORAGE_QUERY_TYPE.PropertyStandardQuery
        };
        if (!DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, ref query, Marshal.SizeOf(query), out var header, Marshal.SizeOf<STORAGE_DEVICE_DESCRIPTOR_HEADER>(), out _, IntPtr.Zero))
        {
            return null;
        }

        var ptr = Marshal.AllocHGlobal((int)header.Size);
        try
        {
            if (!DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, ref query, Marshal.SizeOf(query), ptr, header.Size, out _, IntPtr.Zero))
            {
                return null;
            }

            var descriptor = Marshal.PtrToStructure<STORAGE_DEVICE_DESCRIPTOR>(ptr);
            var rawData = new byte[descriptor.Size];
            Marshal.Copy(ptr, rawData, 0, rawData.Length);
            return new StorageDescriptor(descriptor.BusType, descriptor.RemovableMedia);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}
