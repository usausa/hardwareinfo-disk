namespace ExampleDiskInfo;

using ExampleDiskInfo.Models;

using HardwareInfo.Disk;

public sealed class MainWindowViewModel
{
    public ObservableCollection<DiskInfoModel> Disks { get; } = new();

    public MainWindowViewModel()
    {
        foreach (var disk in DiskInfo.GetInformation())
        {
            Disks.Add(new DiskInfoModel(disk));
        }
    }
}
