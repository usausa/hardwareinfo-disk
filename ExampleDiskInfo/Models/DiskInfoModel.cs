namespace ExampleDiskInfo.Models;

using HardwareInfo.Disk;

public sealed class DiskInfoModel
{
    public IDiskInfo Disk { get; }

    // TODO delete
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once CollectionNeverUpdated.Global
    public ObservableCollection<SmartValue> SmartValues { get; } = new();

    public DiskInfoModel(IDiskInfo disk)
    {
        Disk = disk;
    }

    public void Update()
    {
        SmartValues.Clear();
        // TODO
    }
}
