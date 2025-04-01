namespace ExampleDiskInfo;

using ExampleDiskInfo.Models;

using HardwareInfo.Disk;

public sealed class MainWindowViewModel
{
    public ObservableCollection<DiskInfoModel> Disks { get; } = new();

    public MainWindowViewModel()
    {
        foreach (var disk in DiskInfo.GetInformation().OrderBy(static x => x.GetDrives().FirstOrDefault()?.Name))
        {
            var info = new DiskInfoModel(disk);
            info.Update();
            Disks.Add(info);
        }
    }
}
