using HardwareInfo.Disk;

#pragma warning disable CA1416
foreach (var disk in DiskInfo.GetInformation())
{
    if (disk.DiskType == DiskType.Nvme)
    {
        Console.WriteLine(disk.Model);
        Console.WriteLine($"  Size                   : {disk.Size}");
        var smart = (ISmartNvme)disk.Smart;
        Console.WriteLine($"  DataUnitRead           : {smart.DataUnitRead * 512 * 1000 / 1024 / 1024 / 1024}");
        Console.WriteLine($"  DataUnitWritten        : {smart.DataUnitWritten * 512 * 1000 / 1024 / 1024 / 1024}");
        Console.WriteLine($"  AvailableSpare         : {smart.AvailableSpare}");
        Console.WriteLine($"  PercentageUsed         : {smart.PercentageUsed}");
        Console.WriteLine($"  PowerCycle             : {smart.PowerCycle}");
        Console.WriteLine($"  PowerOnHours           : {smart.PowerOnHours}");
        Console.WriteLine($"  Temperature            : {smart.Temperature}");
        Console.WriteLine($"  UnsafeShutdowns        : {smart.UnsafeShutdowns}");
        Console.WriteLine($"  MediaErrors            : {smart.MediaErrors}");
        Console.WriteLine($"  ErrorInfoLogEntryCount : {smart.ErrorInfoLogEntryCount}");
    }
}

Console.ReadLine();
