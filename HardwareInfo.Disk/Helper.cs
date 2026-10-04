namespace HardwareInfo.Disk;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.NativeMethods;

internal static class Helper
{
    public static short KelvinToCelsius(ushort value) => (short)(value > 0 ? value - 273 : Int16.MinValue);

    public static SafeFileHandle OpenDevice(string devicePath, FileAccess access) =>
        CreateFile(devicePath, access, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
}
