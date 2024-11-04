namespace HardwareInfo.Disk;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
public enum BusType
{
    Unknown = 0x00,
    Scsi,
    Atapi,
    Ata,
    Ieee1394,
    Ssa,
    Fibre,
    Usb,
    Raid,
    IScsi,
    Sas,
    Sata,
    Sd,
    Mmc,
    Virtual,
    FileBackedVirtual,
    Spaces,
    Nvme,
    Scm
}
// ReSharper restore InconsistentNaming
// ReSharper restore IdentifierTypo
