using HardwareInfo.Disk;

#pragma warning disable CA1416
foreach (var disk in DiskInfo.GetInformation())
{
    Console.WriteLine($"[{disk.Model}]");
    Console.WriteLine($"BusType: {disk.BusType}");
    Console.WriteLine($"SmartType: {disk.SmartType}");

    foreach (var partition in disk.GetPartitions())
    {
        Console.WriteLine($"Partition-{partition.Index}: {partition.Name}");
        foreach (var drive in partition.Drives)
        {
            var used = drive.Size - drive.FreeSpace;
            Console.WriteLine($"Drive {drive.Name.TrimEnd(':')}: {used:#,0} / {drive.Size:#,0} ({(double)used * 100 / drive.Size:F2}%)");
        }
    }

    if (disk.SmartType == SmartType.Nvme)
    {
        var smart = (ISmartNvme)disk.Smart;
        Console.WriteLine($"CriticalWarning: {smart.CriticalWarning:X2}");
        Console.WriteLine($"Temperature: {smart.Temperature}");
        Console.WriteLine($"AvailableSpare: {smart.AvailableSpare}");
        Console.WriteLine($"PercentageUsed: {smart.PercentageUsed}");
        Console.WriteLine($"DataRead: {smart.DataUnitRead * 512 * 1000 / 1024 / 1024 / 1024}");
        Console.WriteLine($"DataWrite: {smart.DataUnitWrite * 512 * 1000 / 1024 / 1024 / 1024}");
        Console.WriteLine($"PowerCycle: {smart.PowerCycle}");
        Console.WriteLine($"PowerOnHour: {smart.PowerOnHour}");
        Console.WriteLine($"UnsafeShutdown: {smart.UnsafeShutdown}");
        Console.WriteLine($"MediaError: {smart.MediaError}");
    }
    else if (disk.SmartType == SmartType.Generic)
    {
        var smart = (ISmartGeneric)disk.Smart;
        var ids = smart.GetSupportedIds().ToList();
        Console.WriteLine($"SupportedIds: {String.Join(",", ids.Select(static x => $"{(byte)x:X2}"))}");
        foreach (var id in ids)
        {
            switch (id)
            {
                case SmartId.Temperature:
                    ShowValue(smart, id, static x => x & 0xFF);
                    break;
                default:
                    ShowValue(smart, id, static x => x);
                    break;
            }
        }
    }

    Console.WriteLine();
}

static void ShowValue<TResult>(ISmartGeneric smart, SmartId id, Func<ulong, TResult> converter)
{
    var attr = smart.GetAttribute(id);
    if (attr.HasValue)
    {
        var name = Enum.IsDefined(id) ? $"{(byte)id:X2} {id}" : $"{(byte)id:X2} Undefined";
        Console.WriteLine($"{name}: {attr.Value.RawValue:X12} {converter(attr.Value.RawValue)}");
    }
}

Console.ReadLine();
