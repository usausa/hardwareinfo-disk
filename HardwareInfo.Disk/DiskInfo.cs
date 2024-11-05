namespace HardwareInfo.Disk;

using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.NativeMethods;

[SupportedOSPlatform("windows")]
public static class DiskInfo
{
    public static IReadOnlyList<IDiskInfo> GetInformation()
    {
        var list = new List<IDiskInfo>();

        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
        foreach (var disk in searcher.Get())
        {
            var info = new DiscInfoGeneric
            {
                Index = Convert.ToUInt32(disk.Properties["Index"].Value, CultureInfo.InvariantCulture),
                DeviceId = (string)disk.Properties["DeviceID"].Value,
                PnpDeviceId = (string)disk.Properties["PNPDeviceID"].Value,
                Status = (string)disk.Properties["Status"].Value,
                Model = (string)disk.Properties["Model"].Value,
                SerialNumber = (string)disk.Properties["SerialNumber"].Value,
                FirmwareRevision = (string)disk.Properties["FirmwareRevision"].Value,
                Size = Convert.ToUInt64(disk.Properties["Size"].Value, CultureInfo.InvariantCulture),
                BytesPerSector = Convert.ToUInt32(disk.Properties["BytesPerSector"].Value, CultureInfo.InvariantCulture),
                SectorsPerTrack = Convert.ToUInt32(disk.Properties["SectorsPerTrack"].Value, CultureInfo.InvariantCulture),
                TracksPerCylinder = Convert.ToUInt32(disk.Properties["TracksPerCylinder"].Value, CultureInfo.InvariantCulture),
                TotalHeads = Convert.ToUInt32(disk.Properties["TotalHeads"].Value, CultureInfo.InvariantCulture),
                TotalCylinders = Convert.ToUInt64(disk.Properties["TotalCylinders"].Value, CultureInfo.InvariantCulture),
                TotalTracks = Convert.ToUInt64(disk.Properties["TotalTracks"].Value, CultureInfo.InvariantCulture),
                TotalSectors = Convert.ToUInt64(disk.Properties["TotalSectors"].Value, CultureInfo.InvariantCulture),
                Partitions = Convert.ToUInt32(disk.Properties["Partitions"].Value, CultureInfo.InvariantCulture)
            };
            list.Add(info);

            foreach (var property in disk.Properties)
            {
                Debug.WriteLine($"{property.Name} {property.Value?.GetType()} {property.Value}");
            }

            // Get descriptor
            var descriptor = GetStorageDescriptor(info.DeviceId);
            if (descriptor is null)
            {
                info.SmartType = SmartType.Unsupported;
                info.Smart = SmartUnsupported.Default;
                continue;
            }

            info.BusType = (BusType)descriptor.BusType;
            info.Removable = descriptor.Removable;

            // NVMe
            if (descriptor.BusType is STORAGE_BUS_TYPE.BusTypeNvme)
            {
                info.SmartType = SmartType.Nvme;
                info.Smart = new SmartNvme(OpenDevice(info.DeviceId));
                info.Smart.Update();
                continue;
            }

            // ATA
            if (descriptor.BusType is STORAGE_BUS_TYPE.BusTypeAta or STORAGE_BUS_TYPE.BusTypeSata)
            {
                info.SmartType = SmartType.Generic;
                info.Smart = new SmartGeneric(OpenDevice(info.DeviceId), (byte)info.Index);
                info.Smart.Update();
                continue;
            }

            info.SmartType = SmartType.Unsupported;
            info.Smart = SmartUnsupported.Default;
        }

        list.Sort(IndexComparison);

        return list;
    }

    private static int IndexComparison(IDiskInfo x, IDiskInfo y) => (int)x.Index - (int)y.Index;

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
        var header = default(STORAGE_DEVICE_DESCRIPTOR_HEADER);
        if (!DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, ref query, Marshal.SizeOf(query), ref header, Marshal.SizeOf<STORAGE_DEVICE_DESCRIPTOR_HEADER>(), out _, IntPtr.Zero))
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
