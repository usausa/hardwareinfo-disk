namespace ExampleDiskInfo.Models;

using CommunityToolkit.Mvvm.ComponentModel;

using HardwareInfo.Disk;

public sealed partial class DiskInfoModel : ObservableObject
{
    public IDiskInfo Disk { get; }

    [ObservableProperty]
    public partial int? Health { get; set; }

    [ObservableProperty]
    public partial int? Temperature { get; private set; }

    [ObservableProperty]
    public partial ulong? DataReadGigaBytes { get; private set; }

    [ObservableProperty]
    public partial ulong? DataWriteGigaBytes { get; private set; }

    [ObservableProperty]
    public partial ulong PowerCycles { get; private set; }

    [ObservableProperty]
    public partial ulong PowerOnHours { get; private set; }

    public ObservableCollection<SmartValue> SmartValues { get; } = new();

    public DiskInfoModel(IDiskInfo disk)
    {
        Disk = disk;
    }

    public void Update()
    {
        SmartValues.Clear();

        if (Disk.SmartType == SmartType.Nvme)
        {
            var smart = (ISmartNvme)Disk.Smart;

            Health = 100 - smart.PercentageUsed;
            Temperature = smart.Temperature;
            DataReadGigaBytes = smart.DataUnitRead * 512 * 1000 / 1024 / 1024 / 1024;
            DataWriteGigaBytes = smart.DataUnitWritten * 512 * 1000 / 1024 / 1024 / 1024;
            PowerCycles = smart.PowerCycles;
            PowerOnHours = smart.PowerOnHours;

            SmartValues.Add(new SmartValue(1, nameof(smart.CriticalWarning), smart.CriticalWarning));
            SmartValues.Add(new SmartValue(2, nameof(smart.Temperature), (ulong)smart.Temperature));
            SmartValues.Add(new SmartValue(3, nameof(smart.AvailableSpare), smart.AvailableSpare));
            SmartValues.Add(new SmartValue(4, nameof(smart.AvailableSpareThreshold), smart.AvailableSpareThreshold));
            SmartValues.Add(new SmartValue(5, nameof(smart.PercentageUsed), smart.PercentageUsed));
            SmartValues.Add(new SmartValue(6, nameof(smart.DataUnitRead), smart.DataUnitRead));
            SmartValues.Add(new SmartValue(7, nameof(smart.DataUnitWritten), smart.DataUnitWritten));
            SmartValues.Add(new SmartValue(8, nameof(smart.HostReadCommands), smart.HostReadCommands));
            SmartValues.Add(new SmartValue(9, nameof(smart.HostWriteCommands), smart.HostWriteCommands));
            SmartValues.Add(new SmartValue(10, nameof(smart.ControllerBusyTime), smart.ControllerBusyTime));
            SmartValues.Add(new SmartValue(11, nameof(smart.PowerCycles), smart.PowerCycles));
            SmartValues.Add(new SmartValue(12, nameof(smart.PowerOnHours), smart.PowerOnHours));
            SmartValues.Add(new SmartValue(13, nameof(smart.UnsafeShutdowns), smart.UnsafeShutdowns));
            SmartValues.Add(new SmartValue(14, nameof(smart.MediaErrors), smart.MediaErrors));
            SmartValues.Add(new SmartValue(15, nameof(smart.ErrorInfoLogEntries), smart.ErrorInfoLogEntries));
        }
        else if (Disk.SmartType == SmartType.Generic)
        {
            var smart = (ISmartGeneric)Disk.Smart;

            var health = default(int?);
            var temperature = default(int?);
            var dataReadGigaBytes = default(ulong?);
            var dataWriteGigaBytes = default(ulong?);
            var powerCycles = 0ul;
            var powerOnHours = 0ul;

            foreach (var id in smart.GetSupportedIds())
            {
                var attr = smart.GetAttribute(id);
                if (attr.HasValue)
                {
                    SmartValues.Add(new SmartValue((int)id, id.ToString(), attr.Value.RawValue));

                    switch (id)
                    {
                        case SmartId.Temperature:
                            temperature = (short)(attr.Value.RawValue & 0xFF);
                            break;
                        case SmartId.PowerCycleCount:
                            powerCycles = attr.Value.RawValue;
                            break;
                        case SmartId.PowerOnHours:
                            powerOnHours = attr.Value.RawValue;
                            break;
                        case SmartId.PercentageLifetimeRemaining:
                            health = 100 - (int)attr.Value.RawValue;
                            break;
                    }
                }
            }

            Health = health;
            Temperature = temperature;
            DataReadGigaBytes = dataReadGigaBytes;
            DataWriteGigaBytes = dataWriteGigaBytes;
            PowerCycles = powerCycles;
            PowerOnHours = powerOnHours;
        }
    }
}
