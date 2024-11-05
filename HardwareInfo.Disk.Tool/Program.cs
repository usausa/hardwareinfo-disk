// ReSharper disable UseObjectOrCollectionInitializer
#pragma warning disable IDE0017
#pragma warning disable CA1416
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

using HardwareInfo.Disk;

var rootCommand = new RootCommand("DiskInfo tool");
rootCommand.Handler = CommandHandler.Create((IConsole console) =>
{
    foreach (var disk in DiskInfo.GetInformation())
    {
        console.WriteLine($"Disk-{disk.Index}: {disk.Model}");
        console.WriteLine($"  BusType: {disk.BusType}");
        console.WriteLine($"  SmartType: {disk.SmartType}");

        console.WriteLine($"  Size: {disk.Size:#,0}");
        console.WriteLine($"  BytesPerSector: {disk.BytesPerSector:#,0}");

        console.WriteLine("  Partition:");
        foreach (var partition in disk.GetPartitions())
        {
            console.WriteLine($"    Partition-{partition.Index}: {partition.Name}");
            foreach (var drive in partition.Drives)
            {
                var used = drive.Size - drive.FreeSpace;
                console.WriteLine($"      Drive {drive.Name.TrimEnd(':')}: {used:#,0} / {drive.Size:#,0} ({(double)used * 100 / drive.Size:F2}%)");
            }
        }

        if (disk.SmartType == SmartType.Nvme)
        {
            var smart = (ISmartNvme)disk.Smart;
            console.WriteLine("  SMART:");
            console.WriteLine($"    CriticalWarning: 0x{smart.CriticalWarning:X2}");
            console.WriteLine($"    Temperature: {smart.Temperature}");
            console.WriteLine($"    AvailableSpare: {smart.AvailableSpare}");
            console.WriteLine($"    AvailableSpareThreshold: {smart.AvailableSpare}");
            console.WriteLine($"    PercentageUsed: {smart.PercentageUsed}");
            console.WriteLine($"    DataRead: {smart.DataUnitRead * 512 * 1000 / 1024 / 1024 / 1024}");
            console.WriteLine($"    DataWrite: {smart.DataUnitWrite * 512 * 1000 / 1024 / 1024 / 1024}");
            console.WriteLine($"    HostReadCommand: {smart.HostReadCommand}");
            console.WriteLine($"    HostWriteCommand: {smart.HostWriteCommand}");
            console.WriteLine($"    ControllerBusyTime: {smart.ControllerBusyTime}");
            console.WriteLine($"    PowerCycle: {smart.PowerCycle}");
            console.WriteLine($"    PowerOnHour: {smart.PowerOnHour}");
            console.WriteLine($"    UnsafeShutdown: {smart.UnsafeShutdown}");
            console.WriteLine($"    MediaError: {smart.MediaError}");
            console.WriteLine($"    ErrorInfoLogEntry: {smart.ErrorInfoLogEntry}");
            console.WriteLine($"    WarningCompositeTemperatureTime: {smart.WarningCompositeTemperatureTime}");
            console.WriteLine($"    CriticalCompositeTemperatureTime: {smart.CriticalCompositeTemperatureTime}");
            for (var i = 0; i < smart.TemperatureSensors.Length; i++)
            {
                var value = smart.TemperatureSensors[i];
                if (value > 0)
                {
                    console.WriteLine($"    TemperatureSensors-{i}: {value}");
                }
            }
        }
        else if (disk.SmartType == SmartType.Generic)
        {
            var smart = (ISmartGeneric)disk.Smart;
            console.WriteLine("  SMART:");
            foreach (var id in smart.GetSupportedIds())
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

        void ShowValue<TResult>(ISmartGeneric smart, SmartId id, Func<ulong, TResult> converter)
        {
            var attr = smart.GetAttribute(id);
            if (attr.HasValue)
            {
                var name = Enum.IsDefined(id) ? $"{(byte)id:X2} {id}" : $"{(byte)id:X2} Undefined";
                console.WriteLine($"    {name}: {attr.Value.RawValue:X12} {converter(attr.Value.RawValue)}");
            }
        }
    }
});

var result = await rootCommand.InvokeAsync(args).ConfigureAwait(false);
#if DEBUG
Console.ReadLine();
#endif
return result;
