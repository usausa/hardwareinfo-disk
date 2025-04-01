namespace ExampleDiskInfo.Models;

using CommunityToolkit.Mvvm.ComponentModel;

public sealed partial class SmartValue : ObservableObject
{
    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial ulong RawValue { get; set; }

    public SmartValue(int id, string name, ulong rawValue)
    {
        Id = id;
        Name = name;
        RawValue = rawValue;
    }
}
