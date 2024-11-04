namespace HardwareInfo.Disk;

using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
internal static class NativeMethods
{
    //------------------------------------------------------------------------
    // Const
    //------------------------------------------------------------------------

    public const uint IOCTL_SCSI_PASS_THROUGH = 0x04d004;
    public const uint IOCTL_SCSI_MINIPORT = 0x04d008;
    public const uint IOCTL_SCSI_PASS_THROUGH_DIRECT = 0x04d014;
    public const uint IOCTL_SCSI_GET_ADDRESS = 0x41018;
    public const uint IOCTL_DISK_PERFORMANCE = 0x70020;
    public const uint IOCTL_STORAGE_QUERY_PROPERTY = 0x2D1400;
    public const uint IOCTL_BATTERY_QUERY_TAG = 0x294040;
    public const uint IOCTL_BATTERY_QUERY_INFORMATION = 0x294044;
    public const uint IOCTL_BATTERY_QUERY_STATUS = 0x29404C;

    //------------------------------------------------------------------------
    // Enum
    //------------------------------------------------------------------------

    public enum STORAGE_PROPERTY_ID
    {
        StorageDeviceProperty = 0,
        StorageAdapterProperty,
        StorageDeviceIdProperty,
        StorageDeviceUniqueIdProperty,
        StorageDeviceWriteCacheProperty,
        StorageMiniportProperty,
        StorageAccessAlignmentProperty,
        StorageDeviceSeekPenaltyProperty,
        StorageDeviceTrimProperty,
        StorageDeviceWriteAggregationProperty,
        StorageDeviceDeviceTelemetryProperty,
        StorageDeviceLBProvisioningProperty,
        StorageDevicePowerProperty,
        StorageDeviceCopyOffloadProperty,
        StorageDeviceResiliencyProperty,
        StorageDeviceMediumProductType,
        StorageAdapterRpmbProperty,
        StorageDeviceIoCapabilityProperty = 48,
        StorageAdapterProtocolSpecificProperty,
        StorageDeviceProtocolSpecificProperty,
        StorageAdapterTemperatureProperty,
        StorageDeviceTemperatureProperty,
        StorageAdapterPhysicalTopologyProperty,
        StorageDevicePhysicalTopologyProperty,
        StorageDeviceAttributesProperty,
        StorageDeviceManagementStatus,
        StorageAdapterSerialNumberProperty,
        StorageDeviceLocationProperty,
        StorageDeviceNumaProperty,
        StorageDeviceZonedDeviceProperty,
        StorageDeviceUnsafeShutdownCount,
        StorageDeviceEnduranceProperty,
        StorageDeviceLedStateProperty,
        StorageDeviceSelfEncryptionProperty = 64,
        StorageFruIdProperty,
        StorageStackProperty,
        StorageAdapterProtocolSpecificPropertyEx,
        StorageDeviceProtocolSpecificPropertyEx
    }

    public enum STORAGE_QUERY_TYPE
    {
        PropertyStandardQuery = 0,
        PropertyExistsQuery,
        PropertyMaskQuery,
        PropertyQueryMaxDefined
    }

    public enum STORAGE_BUS_TYPE
    {
        BusTypeUnknown = 0x00,
        BusTypeScsi,
        BusTypeAtapi,
        BusTypeAta,
        BusType1394,
        BusTypeSsa,
        BusTypeFibre,
        BusTypeUsb,
        BusTypeRAID,
        BusTypeiScsi,
        BusTypeSas,
        BusTypeSata,
        BusTypeSd,
        BusTypeMmc,
        BusTypeVirtual,
        BusTypeFileBackedVirtual,
        BusTypeSpaces,
        BusTypeNvme,
        BusTypeSCM,
        BusTypeMax,
        BusTypeMaxReserved = 0x7F
    }

    public enum STORAGE_PROTOCOL_TYPE
    {
        ProtocolTypeUnknown = 0x00,
        ProtocolTypeScsi,
        ProtocolTypeAta,
        ProtocolTypeNvme,
        ProtocolTypeSd,
        ProtocolTypeProprietary = 0x7E,
        ProtocolTypeMaxReserved = 0x7F
    }

    public enum STORAGE_PROTOCOL_NVME_DATA_TYPE
    {
        NVMeDataTypeUnknown = 0,
        NVMeDataTypeIdentify,
        NVMeDataTypeLogPage,
        NVMeDataTypeFeature
    }

    public enum NVME_LOG_PAGES
    {
        NVME_LOG_PAGE_ERROR_INFO = 0x01,
        NVME_LOG_PAGE_HEALTH_INFO = 0x02,
        NVME_LOG_PAGE_FIRMWARE_SLOT_INFO = 0x03,
        NVME_LOG_PAGE_CHANGED_NAMESPACE_LIST = 0x04,
        NVME_LOG_PAGE_COMMAND_EFFECTS = 0x05,
        NVME_LOG_PAGE_DEVICE_SELF_TEST = 0x06,
        NVME_LOG_PAGE_TELEMETRY_HOST_INITIATED = 0x07,
        NVME_LOG_PAGE_TELEMETRY_CTLR_INITIATED = 0x08,
        NVME_LOG_PAGE_RESERVATION_NOTIFICATION = 0x80,
        NVME_LOG_PAGE_SANITIZE_STATUS = 0x81
    }

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct STORAGE_PROPERTY_QUERY
    {
        public STORAGE_PROPERTY_ID PropertyId;
        public STORAGE_QUERY_TYPE QueryType;
        public fixed byte AdditionalParameters[1];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STORAGE_DEVICE_DESCRIPTOR_HEADER
    {
        public uint Version;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STORAGE_DEVICE_DESCRIPTOR
    {
        public uint Version;
        public uint Size;
        public byte DeviceType;
        public byte DeviceTypeModifier;
        [MarshalAs(UnmanagedType.U1)]
        public bool RemovableMedia;
        [MarshalAs(UnmanagedType.U1)]
        public bool CommandQueueing;
        public uint VendorIdOffset;
        public uint ProductIdOffset;
        public uint ProductRevisionOffset;
        public uint SerialNumberOffset;
        public STORAGE_BUS_TYPE BusType;
        public uint RawPropertiesLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STORAGE_PROTOCOL_SPECIFIC_DATA
    {
        public STORAGE_PROTOCOL_TYPE ProtocolType;
        public uint DataType;
        public uint ProtocolDataRequestValue;
        public uint ProtocolDataRequestSubValue;
        public uint ProtocolDataOffset;
        public uint ProtocolDataLength;
        public uint FixedProtocolReturnData;
        public uint ProtocolDataRequestSubValue2;
        public uint ProtocolDataRequestSubValue3;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct STORAGE_QUERY_BUFFER
    {
        public STORAGE_PROPERTY_ID PropertyId;
        public STORAGE_QUERY_TYPE QueryType;
        public STORAGE_PROTOCOL_SPECIFIC_DATA ProtocolSpecific;
        public fixed byte Buffer[4096];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NVME_HEALTH_INFO_LOG
    {
        public byte CriticalWarning;
        public fixed byte CompositeTemp[2];
        public byte AvailableSpare;
        public byte AvailableSpareThreshold;
        public byte PercentageUsed;
        public fixed byte Reserved1[26];
        public fixed byte DataUnitRead[16];
        public fixed byte DataUnitWritten[16];
        public fixed byte HostReadCommands[16];
        public fixed byte HostWriteCommands[16];
        public fixed byte ControllerBusyTime[16];
        public fixed byte PowerCycles[16];
        public fixed byte PowerOnHours[16];
        public fixed byte UnsafeShutdowns[16];
        public fixed byte MediaAndDataIntegrityErrors[16];
        public fixed byte NumberErrorInformationLogEntries[16];
        public uint WarningCompositeTemperatureTime;
        public uint CriticalCompositeTemperatureTime;
        public fixed ushort TemperatureSensor[8];
        public fixed byte Reserved2[296];
    }

    //------------------------------------------------------------------------
    // Method
    //------------------------------------------------------------------------

    private const string Kernel32 = "kernel32.dll";

    [DllImport(Kernel32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern SafeFileHandle CreateFile(
        [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
        [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
        [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
        [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport(Kernel32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
        SafeHandle hDevice,
        uint dwIoControlCode,
        ref STORAGE_PROPERTY_QUERY lpInBuffer,
        int nInBufferSize,
        out STORAGE_DEVICE_DESCRIPTOR_HEADER lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(Kernel32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
        SafeHandle hDevice,
        uint dwIoControlCode,
        ref STORAGE_PROPERTY_QUERY lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(Kernel32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
        SafeHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);
}
// ReSharper restore InconsistentNaming
// ReSharper restore IdentifierTypo
