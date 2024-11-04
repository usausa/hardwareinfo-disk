using HardwareInfo.Disk;

#pragma warning disable CA1416
foreach (var disk in DiskInfo.GetInformation())
{
    if (disk.SmartType == SmartType.Nvme)
    {
        var smart = (ISmartNvme)disk.Smart;
        Console.WriteLine($"{disk.Model} : NVMe");
        Console.WriteLine($"  DataRead                             : {smart.DataUnitRead * 512 * 1000 / 1024 / 1024 / 1024}");
        Console.WriteLine($"  DataWritten                          : {smart.DataUnitWritten * 512 * 1000 / 1024 / 1024 / 1024}");
        Console.WriteLine($"  AvailableSpare                       : {smart.AvailableSpare}");
        Console.WriteLine($"  PercentageUsed                       : {smart.PercentageUsed}");
        Console.WriteLine($"  MediaErrors                          : {smart.MediaErrors}");
        Console.WriteLine($"  PowerCycle                           : {smart.PowerCycle}");
        Console.WriteLine($"  PowerOnHours                         : {smart.PowerOnHours}");
        Console.WriteLine($"  Temperature                          : {smart.Temperature}");
        Console.WriteLine($"  UnsafeShutdowns                      : {smart.UnsafeShutdowns}");
    }
    else if (disk.SmartType == SmartType.Generic)
    {
        var smart = (ISmartGeneric)disk.Smart;
        Console.WriteLine($"{disk.Model} : Generic : {String.Join(",", smart.GetSupportedIds().Select(static x => $"{(byte)x:X2}"))}");
        ShowValue(smart, SmartId.RawReadErrorRate, static x => x);
        ShowValue(smart, SmartId.ReallocatedSectorsCount, static x => x);
        ShowValue(smart, SmartId.PowerOnHours, static x => x);
        ShowValue(smart, SmartId.DevicePowerCycleCount, static x => x);
        ShowValue(smart, SmartId.CurrentHeliumLevel, static x => x);
        ShowValue(smart, SmartId.Temperature, static x => x & 0xFF);
        ShowValue(smart, SmartId.ReallocationEventCount, static x => x & 0xFF);
        ShowValue(smart, SmartId.CurrentPendingSectorCount, static x => x & 0xFF);
        ShowValue(smart, SmartId.OffLineScanUncorrectableSectorCount, static x => x & 0xFF);
        ShowValue(smart, SmartId.UltraDmaCrcErrorCount, static x => x & 0xFF);
        ShowValue(smart, SmartId.DiskShift, static x => x & 0xFF);
    }
    else
    {
        Console.WriteLine($"{disk.Model} : Unsupported");
    }
}

static void ShowValue<TResult>(ISmartGeneric smart, SmartId id, Func<ulong, TResult> converter)
{
    var attr = smart.GetAttribute(id);
    if (attr.HasValue)
    {
        Console.WriteLine($"  {id.ToString(),-36} : {converter(attr.Value.RawValue)}");
    }
}

Console.ReadLine();
