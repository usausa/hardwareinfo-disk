namespace HardwareInfo.Disk;

using System.Globalization;
using System.Management;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
public static class DiskInfoExtensions
{
    public static IEnumerable<PartitionInfo> GetPartitions(this IDiskInfo disk)
    {
        using var partitions = new ManagementObjectSearcher(
            $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{disk.DeviceId}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");
        foreach (var partition in partitions.Get())
        {
            var partitionInfo = new PartitionInfo
            {
                Index = Convert.ToUInt32(partition.Properties["Index"].Value, CultureInfo.InvariantCulture),
                DeviceId = (string)partition.Properties["DeviceID"].Value,
                Name = (string)partition.Properties["Name"].Value,
                Size = Convert.ToUInt64(partition.Properties["Size"].Value, CultureInfo.InvariantCulture)
            };

            using var drives = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
            foreach (var drive in drives.Get())
            {
                var driveInfo = new DriveInfo
                {
                    Partition = partitionInfo,
                    DeviceId = (string)drive.Properties["DeviceID"].Value,
                    Name = (string)drive.Properties["Name"].Value,
                    FileSystem = (string)drive.Properties["FileSystem"].Value,
                    Size = Convert.ToUInt64(drive.Properties["Size"].Value, CultureInfo.InvariantCulture),
                    FreeSpace = Convert.ToUInt64(drive.Properties["FreeSpace"].Value, CultureInfo.InvariantCulture)
                };

                partitionInfo.AddDrive(driveInfo);
            }

            yield return partitionInfo;
        }
    }

    public static IEnumerable<DriveInfo> GetDrives(this IDiskInfo disk) =>
        disk.GetPartitions().SelectMany(static x => x.Drives);
}
