namespace HardwareInfo.Disk;

internal static class Helper
{
    public static short KelvinToCelsius(ushort value) => (short)(value > 0 ? value - 273 : Int16.MinValue);

    public static short KelvinToCelsius(byte[] value) => KelvinToCelsius(BitConverter.ToUInt16(value, 0));
}
