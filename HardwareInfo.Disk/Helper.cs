namespace HardwareInfo.Disk;

internal static class Helper
{
    public static short KelvinToCelsius(ushort value) => (short)(value > 0 ? value - 273 : Int16.MinValue);
}
