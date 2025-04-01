# Disk information library

| Package | Info |
|:-|:-|
| HardwareInfo.Disk | [![NuGet](https://img.shields.io/nuget/v/HardwareInfo.Disk.svg)](https://www.nuget.org/packages/HardwareInfo.Disk) |

## Supported

| Type | Support |
|:-|:-|
| NVMe | ✅ |
| SATA | ✅ |
| USB(SAT12 only) | ✅ |

## Example

![example](https://github.com/usausa/hardwareinfo-disk/blob/main/Document/example.png)

## Usage

```csharp
using HardwareInfo.Disk;

foreach (var disk in DiskInfo.GetInformation())
{
    Console.WriteLine($"Disk-{disk.Index} {disk.Model}");
    Console.WriteLine($"BusType : {disk.BusType}");
    Console.WriteLine($"SmartType : {disk.SmartType}");
    Console.WriteLine($"Size : {disk.Size:#,0}");
    Console.WriteLine($"Partitions : {disk.Partitions}");
    foreach (var partition in disk.GetPartitions())
    {
        Console.WriteLine($"  Partition-{partition.Index} : {partition.Name}");
        foreach (var drive in partition.Drives)
        {
            var used = drive.Size - drive.FreeSpace;
            Console.WriteLine($"    Drive[{drive.Name}] : {used:#,0} / {drive.Size:#,0} ({(double)used * 100 / drive.Size:F2}%)");
        }
    }

    Console.WriteLine("Smart:");
    if (disk.SmartType == SmartType.Nvme)
    {
        var smart = (ISmartNvme)disk.Smart;

        Console.WriteLine($"  CriticalWarning: {smart.CriticalWarning:X2}");
        Console.WriteLine($"  Temperature: {smart.Temperature}");
        Console.WriteLine($"  AvailableSpare: {smart.AvailableSpare}");
        Console.WriteLine($"  AvailableSpareThreshold: {smart.AvailableSpare}");
        Console.WriteLine($"  PercentageUsed: {smart.PercentageUsed}");
        Console.WriteLine($"  DataRead(GB): {smart.DataUnitRead * 512 * 1000 / 1024 / 1024 / 1024}");
        Console.WriteLine($"  DataWrite(GB): {smart.DataUnitWritten * 512 * 1000 / 1024 / 1024 / 1024}");
        Console.WriteLine($"  PowerCycles: {smart.PowerCycles}");
        Console.WriteLine($"  PowerOnHours: {smart.PowerOnHours}");
    }
    else if (disk.SmartType == SmartType.Generic)
    {
        var smart = (ISmartGeneric)disk.Smart;

        foreach (var id in smart.GetSupportedIds())
        {
            var attr = smart.GetAttribute(id);
            if (attr.HasValue)
            {
                var name = Enum.IsDefined(id) ? $"{(byte)id:X2} {id}" : $"{(byte)id:X2} Undefined";
                var value = id == SmartId.Temperature ? attr.Value.RawValue & 0xFF : attr.Value.RawValue;
                Console.WriteLine($"  {name}: {value}");
            }
        }
    }
}
```

## Result example

```
Disk-0 CT1000MX500SSD1
BusType : Sata
SmartType : Generic
Size : 1,000,202,273,280
Partitions : 1
  Partition-0 : ディスク #0, パーティション #0
    Drive[D:] : 482,642,022,400 / 1,000,186,310,656 (48.26%)
Smart:
  01 RawReadErrorRate: 0
  05 ReallocatedSectorCount: 0
  09 PowerOnHours: 3861
  0C PowerCycleCount: 55
  AB ProgramFailCount: 0
  AC EraseFailCount: 0
  AD AverageBlockEraseCount: 109
  AE UnexpectedPowerLoss: 22
  B4 Undefined: 28
  B7 Undefined: 0
  B8 ErrorCorrectionCount: 0
  BB ReportedUncorrectableErrors: 0
  C2 Temperature: 32
  C4 ReallocationEventCount: 0
  C5 CurrentPendingSectorCount: 0
  C6 UncorrectableSectorCount: 0
  C7 UltraDmaCrcErrorCount: 0
  CA PercentageLifetimeRemaining: 7
  CE Undefined: 0
  D2 Undefined: 0
  F6 TotalHostSectorWrite: 46171673702
  F7 Undefined: 516005198
  F8 Undefined: 464786042
  80 Undefined: 2199040033659
  02 ThroughputPerformance: 0
  4D Undefined: 2110003
  52 Undefined: 0
Disk-1 CT1000P1SSD8
BusType : Nvme
SmartType : Nvme
Size : 1,000,202,273,280
Partitions : 4
  Partition-0 : ディスク #1, パーティション #0
  Partition-1 : ディスク #1, パーティション #1
  Partition-2 : ディスク #1, パーティション #2
    Drive[C:] : 79,132,274,688 / 998,838,890,496 (7.92%)
  Partition-3 : ディスク #1, パーティション #3
Smart:
  CriticalWarning: 00
  Temperature: 35
  AvailableSpare: 100
  AvailableSpareThreshold: 100
  PercentageUsed: 4
  DataRead(GB): 2374
  DataWrite(GB): 11711
  PowerCycles: 53
  PowerOnHours: 40393
Disk-2 WDC WD80 EFPX-68C4ZN0 USB Device
BusType : Usb
SmartType : Generic
Size : 8,001,560,609,280
Partitions : 1
  Partition-0 : ディスク #2, パーティション #0
    Drive[E:] : 5,804,234,248,192 / 8,001,545,039,872 (72.54%)
Smart:
  01 RawReadErrorRate: 0
  03 SpinUpTime: 2900
  04 StartStopCount: 28
  05 ReallocatedSectorCount: 0
  07 SeekErrorRate: 0
  09 PowerOnHours: 2343
  0A SpinRetryCount: 0
  0B RecalibrationRetries: 0
  0C PowerCycleCount: 7
  C0 PowerOffRetractCount: 2
  C1 LoadUnloadCycleCount: 157
  C2 Temperature: 26
  C4 ReallocationEventCount: 0
  C5 CurrentPendingSectorCount: 0
  C6 UncorrectableSectorCount: 0
  C7 UltraDmaCrcErrorCount: 0
  C8 Undefined: 0
Disk-3 WDC WD60 EFRX-68L0BN1 USB Device
BusType : Usb
SmartType : Generic
Size : 6,001,172,513,280
Partitions : 1
  Partition-0 : ディスク #3, パーティション #0
    Drive[F:] : 5,962,624,000,000 / 6,001,143,054,336 (99.36%)
Smart:
  01 RawReadErrorRate: 0
  03 SpinUpTime: 7483
  04 StartStopCount: 170
  05 ReallocatedSectorCount: 0
  07 SeekErrorRate: 0
  09 PowerOnHours: 20321
  0A SpinRetryCount: 0
  0B RecalibrationRetries: 0
  0C PowerCycleCount: 47
  C0 PowerOffRetractCount: 28
  C1 LoadUnloadCycleCount: 1482
  C2 Temperature: 26
  C4 ReallocationEventCount: 0
  C5 CurrentPendingSectorCount: 0
  C6 UncorrectableSectorCount: 0
  C7 UltraDmaCrcErrorCount: 0
  C8 Undefined: 0
```

## Globalt tool

### Install

```
> dotnet tool install -g HardwareInfo.Disk.Tool
```

### Usage

Run as administrator.

```
diskinfo
```

## Link

- [Prometheus Expoerter alternative](https://github.com/usausa/prometheus-exporter-alternative)
