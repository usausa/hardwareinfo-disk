# Disk information library

| Package | Info |
|:-|:-|
| HardwareInfo.Disk | [![NuGet Badge](https://buildstats.info/nuget/HardwareInfo.Disk)](https://www.nuget.org/packages/HardwareInfo.Disk/) |

## Supported

| Type | Support |
|:-|:-|
| NVMe | ✅ |
| SATA | ✅ |
| USB(SAT only) | (TODO) |

## Usage

```csharp
using HardwareInfo.Disk;

foreach (var disk in DiskInfo.GetInformation())
{
    Console.WriteLine(disk.Model);

    if (disk.SmartType == SmartType.Nvme)
    {
        var smart = (ISmartNvme)disk.Smart;

        Console.WriteLine($"Temperature: {smart.Temperature}");
    }
    else if (disk.SmartType == SmartType.Generic)
    {
        var smart = (ISmartGeneric)disk.Smart;

        Console.WriteLine($"Temperature: {smart.GetAttribute(SmartId.Temperature)?.RawValue}");
    }
}
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
