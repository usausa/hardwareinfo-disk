using HardwareInfo.Disk;

foreach (var storage in DiskInfo.GetInformation())
{
    //Console.WriteLine(storage.DeviceId);
}

Console.ReadLine();
