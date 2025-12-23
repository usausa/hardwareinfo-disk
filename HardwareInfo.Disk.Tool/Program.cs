#pragma warning disable CA1416
using HardwareInfo.Disk;

using Smart.CommandLine.Hosting;

var builder = CommandHost.CreateBuilder(args);
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("DiskInfo tool");
        root.Configure(cmd => cmd.SetAction(_ => DisplayInfo()));
    });
});

var host = builder.Build();
return await host.RunAsync();

static void DisplayInfo()
{
    foreach (var disk in DiskInfo.GetInformation())
    {
        Console.WriteLine($"Disk-{disk.Index} {disk.Model}");
        Console.WriteLine($"  BusType        : {disk.BusType}");
        Console.WriteLine($"  SmartType      : {disk.SmartType}");

        Console.WriteLine($"  Size           : {disk.Size:#,0}");
        Console.WriteLine($"  BytesPerSector : {disk.BytesPerSector:#,0}");

        Console.WriteLine($"  Partitions     : {disk.Partitions}");
        foreach (var partition in disk.GetPartitions())
        {
            Console.WriteLine($"    Partition-{partition.Index} : {partition.Name}");
            foreach (var drive in partition.Drives)
            {
                var used = drive.Size - drive.FreeSpace;
                Console.WriteLine($"      Drive[{drive.Name}] : {used:#,0} / {drive.Size:#,0} ({(double)used * 100 / drive.Size:F2}%)");
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
                new("DataWrite(GB)", $"{smart.DataUnitWritten * 512 * 1000 / 1024 / 1024 / 1024}"),
                new("HostReadCommands", $"{smart.HostReadCommands}"),
                new("HostWriteCommands", $"{smart.HostWriteCommands}"),
                new("ControllerBusyTime", $"{smart.ControllerBusyTime}"),
                new("PowerCycles", $"{smart.PowerCycles}"),
                new("PowerOnHours", $"{smart.PowerOnHours}"),
                new("UnsafeShutdowns", $"{smart.UnsafeShutdowns}"),
                new("MediaErrors", $"{smart.MediaErrors}"),
                new("ErrorInfoLogEntries", $"{smart.ErrorInfoLogEntries}"),
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

            Console.WriteLine("  SMART:");
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

            Console.WriteLine("  SMART:");
            ShowSmartTable(rows);

            string ToKey(SmartId id) => Enum.IsDefined(id) ? $"{(byte)id:X2} {id}" : $"{(byte)id:X2} Undefined";

            string ToValue<T>(SmartAttribute attr, Func<ulong, T> converter) => $"{attr.RawValue:X12} {converter(attr.RawValue)}";
        }

        void ShowSmartTable(List<KeyValuePair<string, string>> rows)
        {
            var maxKey = rows.Max(static x => x.Key.Length);
            foreach (var row in rows)
            {
                Console.WriteLine($"    {row.Key.PadRight(maxKey)} : {row.Value}");
            }
        }
    }
}
