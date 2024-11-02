using HardwareInfo.Smart;

foreach (var storage in StorageInformation.GetInformation())
{
    //Console.WriteLine(storage.DeviceId);
}

Console.ReadLine();
