namespace ExampleDiskInfo.Models;

public sealed class SmartValue
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public long RawValue { get; set; }
}
