namespace ExampleDiskInfo.Converters;

using HardwareInfo.Disk;

[ValueConversion(typeof(IDiskInfo), typeof(string))]
public sealed class DriveLetterConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IDiskInfo disk)
        {
            return String.Join(", ", disk.GetDrives().Select(static x => x.Name));
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
