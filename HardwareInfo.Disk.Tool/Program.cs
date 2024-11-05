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
        console.WriteLine($"Disk-{disk.Index} {disk.Model}");
        console.WriteLine($"  BusType        : {disk.BusType}");
        console.WriteLine($"  SmartType      : {disk.SmartType}");

        console.WriteLine($"  Size           : {disk.Size:#,0}");
        console.WriteLine($"  BytesPerSector : {disk.BytesPerSector:#,0}");

        console.WriteLine($"  Partitions     : {disk.Partitions}");
        foreach (var partition in disk.GetPartitions())
        {
            console.WriteLine($"    Partition-{partition.Index} : {partition.Name}");
            foreach (var drive in partition.Drives)
            {
                var used = drive.Size - drive.FreeSpace;
                console.WriteLine($"      Drive[{drive.Name}] : {used:#,0} / {drive.Size:#,0} ({(double)used * 100 / drive.Size:F2}%)");
            }
        }

        if (disk.SmartType == SmartType.Nvme)
        {
            var smart = (ISmartNvme)disk.Smart;

            var rows = new List<KeyValuePair<string, string>>
            {
                new("CriticalWarning", $"{smart.CriticalWarning:X2}"),
                new("Temperature", $"{smart.Temperature}"),
                new("AvailableSpare", $"{smart.AvailableSpare}"),
                new("AvailableSpareThreshold", $"{smart.AvailableSpare}"),
                new("PercentageUsed", $"{smart.PercentageUsed}"),
                new("DataRead(GB)", $"{smart.DataUnitRead * 512 * 1000 / 1024 / 1024 / 1024}"),
                new("DataWrite(GB)", $"{smart.DataUnitWrite * 512 * 1000 / 1024 / 1024 / 1024}"),
                new("HostReadCommand", $"{smart.HostReadCommand}"),
                new("HostWriteCommand", $"{smart.HostWriteCommand}"),
                new("ControllerBusyTime", $"{smart.ControllerBusyTime}"),
                new("PowerCycle", $"{smart.PowerCycle}"),
                new("PowerOnHour", $"{smart.PowerOnHour}"),
                new("UnsafeShutdown", $"{smart.UnsafeShutdown}"),
                new("MediaError", $"{smart.MediaError}"),
                new("ErrorInfoLogEntry", $"{smart.ErrorInfoLogEntry}"),
                new("WarningCompositeTemperatureTime", $"{smart.WarningCompositeTemperatureTime}"),
                new("CriticalCompositeTemperatureTime", $"{smart.CriticalCompositeTemperatureTime}")
            };
            for (var i = 0; i < smart.TemperatureSensors.Length; i++)
            {
                var value = smart.TemperatureSensors[i];
                if (value > 0)
                {
                    rows.Add(new($"TemperatureSensors-{i}", $"{value}"));
                }
            }

            console.WriteLine("  SMART:");
            ShowSmartTable(rows);
        }
        else if (disk.SmartType == SmartType.Generic)
        {
            var smart = (ISmartGeneric)disk.Smart;

            var rows = new List<KeyValuePair<string, string>>();
            foreach (var id in smart.GetSupportedIds())
            {
                var attr = smart.GetAttribute(id);
                if (attr.HasValue)
                {
                    switch (id)
                    {
                        case SmartId.Temperature:
                            rows.Add(new(ToKey(id), ToValue(attr.Value, static x => x & 0xFF)));
                            break;
                        default:
                            rows.Add(new(ToKey(id), ToValue(attr.Value, static x => x)));
                            break;
                    }
                }
            }

            console.WriteLine("  SMART:");
            ShowSmartTable(rows);

            string ToKey(SmartId id) => Enum.IsDefined(id) ? $"{(byte)id:X2} {id}" : $"{(byte)id:X2} Undefined";

            string ToValue<T>(SmartAttribute attr, Func<ulong, T> converter) => $"{attr.RawValue:X12} {converter(attr.RawValue)}";
        }

        void ShowSmartTable(List<KeyValuePair<string, string>> rows)
        {
            var maxKey = rows.Max(static x => x.Key.Length);
            foreach (var row in rows)
            {
                console.WriteLine($"    {row.Key.PadRight(maxKey)} : {row.Value}");
            }
        }
    }
});

var result = await rootCommand.InvokeAsync(args).ConfigureAwait(false);
#if DEBUG
Console.ReadLine();
#endif
return result;
