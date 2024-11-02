namespace HardwareInfo.Storage;

using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

public static class Kernel32
{
    private const string DllName = "kernel32.dll";

#pragma warning disable CA2000
    internal static SafeFileHandle? OpenDevice(string devicePath)
    {
        var hDevice = CreateFile(devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
        if (hDevice.IsInvalid || hDevice.IsClosed)
        {
            return null;
        }

        return hDevice;
    }
#pragma warning restore CA2000

#pragma warning disable CA2101
    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern SafeFileHandle CreateFile(
        [MarshalAs(UnmanagedType.LPTStr)] string lpFileName,
        [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
        [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
        [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
        IntPtr hTemplateFile);
#pragma warning restore CA2101

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl(
        SafeHandle hDevice,
        IOCTL dwIoControlCode,
        ref STORAGE_PROPERTY_QUERY lpInBuffer,
        int nInBufferSize,
        out STORAGE_DEVICE_DESCRIPTOR_HEADER lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl(
        SafeHandle hDevice,
        IOCTL dwIoControlCode,
        ref STORAGE_PROPERTY_QUERY lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl
    (
        SafeHandle hDevice,
        IOCTL dwIoControlCode,
        IntPtr lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    // TODO 必要？
    internal static T CreateStruct<T>()
    {
        int size = Marshal.SizeOf<T>();
        IntPtr ptr = Marshal.AllocHGlobal(size);
        RtlZeroMemory(ptr, size);
        T result = Marshal.PtrToStructure<T>(ptr)!;
        Marshal.FreeHGlobal(ptr);
        return result;
    }

    [DllImport(DllName, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern void RtlZeroMemory(IntPtr Destination, int Length);

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_PROPERTY_QUERY
    {
        public STORAGE_PROPERTY_ID PropertyId;
        public STORAGE_QUERY_TYPE QueryType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] AdditionalParameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_DEVICE_DESCRIPTOR_HEADER
    {
        public uint Version;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_DEVICE_DESCRIPTOR
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
    internal struct NVME_POWER_STATE_DESC
    {
        /// <summary>
        /// bit 0:15 Maximum  Power (MP) in centiwatts
        /// </summary>
        public ushort MP;

        /// <summary>
        /// bit 16:23
        /// </summary>
        public byte Reserved0;

        /// <summary>
        /// bit 24 Max Power Scale (MPS), bit 25 Non-Operational State (NOPS)
        /// </summary>
        public byte MPS_NOPS;

        /// <summary>
        /// bit 32:63 Entry Latency (ENLAT) in microseconds
        /// </summary>
        public uint ENLAT;

        /// <summary>
        /// bit 64:95 Exit Latency (EXLAT) in microseconds
        /// </summary>
        public uint EXLAT;

        /// <summary>
        /// bit 96:100 Relative Read Throughput (RRT)
        /// </summary>
        public byte RRT;

        /// <summary>
        /// bit 104:108 Relative Read Latency (RRL)
        /// </summary>
        public byte RRL;

        /// <summary>
        /// bit 112:116 Relative Write Throughput (RWT)
        /// </summary>
        public byte RWT;

        /// <summary>
        /// bit 120:124 Relative Write Latency (RWL)
        /// </summary>
        public byte RWL;

        /// <summary>
        /// bit 128:143 Idle Power (IDLP)
        /// </summary>
        public ushort IDLP;

        /// <summary>
        /// bit 150:151 Idle Power Scale (IPS)
        /// </summary>
        public byte IPS;

        /// <summary>
        /// bit 152:159
        /// </summary>
        public byte Reserved7;

        /// <summary>
        /// bit 160:175 Active Power (ACTP)
        /// </summary>
        public ushort ACTP;

        /// <summary>
        /// bit 176:178 Active Power Workload (APW), bit 182:183  Active Power Scale (APS)
        /// </summary>
        public byte APW_APS;

        /// <summary>
        /// bit 184:255.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public byte[] Reserved9;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NVME_IDENTIFY_CONTROLLER_DATA
    {
        /// <summary>
        /// byte 0:1 M - PCI Vendor ID (VID)
        /// </summary>
        public ushort VID;

        /// <summary>
        /// byte 2:3 M - PCI Subsystem Vendor ID (SSVID)
        /// </summary>
        public ushort SSVID;

        /// <summary>
        /// byte 4: 23 M - Serial Number (SN)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] SN;

        /// <summary>
        /// byte 24:63 M - Model Number (MN)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] MN;

        /// <summary>
        /// byte 64:71 M - Firmware Revision (FR)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] FR;

        /// <summary>
        /// byte 72 M - Recommended Arbitration Burst (RAB)
        /// </summary>
        public byte RAB;

        /// <summary>
        /// byte 73:75 M - IEEE OUI Identifier (IEEE). Controller Vendor code.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] IEEE;

        /// <summary>
        /// byte 76 O - Controller Multi-Path I/O and Namespace Sharing Capabilities (CMIC)
        /// </summary>
        public byte CMIC;

        /// <summary>
        /// byte 77 M - Maximum Data Transfer Size (MDTS)
        /// </summary>
        public byte MDTS;

        /// <summary>
        /// byte 78:79 M - Controller ID (CNTLID)
        /// </summary>
        public ushort CNTLID;

        /// <summary>
        /// byte 80:83 M - Version (VER)
        /// </summary>
        public uint VER;

        /// <summary>
        /// byte 84:87 M - RTD3 Resume Latency (RTD3R)
        /// </summary>
        public uint RTD3R;

        /// <summary>
        /// byte 88:91 M - RTD3 Entry Latency (RTD3E)
        /// </summary>
        public uint RTD3E;

        /// <summary>
        /// byte 92:95 M - Optional Asynchronous Events Supported (OAES)
        /// </summary>
        public uint OAES;

        /// <summary>
        /// byte 96:239.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 144)]
        public byte[] Reserved0;

        /// <summary>
        /// byte 240:255.  Refer to the NVMe Management Interface Specification for definition.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ReservedForManagement;

        /// <summary>
        /// byte 256:257 M - Optional Admin Command Support (OACS)
        /// </summary>
        public ushort OACS;

        /// <summary>
        /// byte 258 M - Abort Command Limit (ACL)
        /// </summary>
        public byte ACL;

        /// <summary>
        /// byte 259 M - Asynchronous Event Request Limit (AERL)
        /// </summary>
        public byte AERL;

        /// <summary>
        /// byte 260 M - Firmware Updates (FRMW)
        /// </summary>
        public byte FRMW;

        /// <summary>
        /// byte 261 M - Log Page Attributes (LPA)
        /// </summary>
        public byte LPA;

        /// <summary>
        /// byte 262 M - Error Log Page Entries (ELPE)
        /// </summary>
        public byte ELPE;

        /// <summary>
        /// byte 263 M - Number of Power States Support (NPSS)
        /// </summary>
        public byte NPSS;

        /// <summary>
        /// byte 264 M - Admin Vendor Specific Command Configuration (AVSCC)
        /// </summary>
        public byte AVSCC;

        /// <summary>
        /// byte 265 O - Autonomous Power State Transition Attributes (APSTA)
        /// </summary>
        public byte APSTA;

        /// <summary>
        /// byte 266:267 M - Warning Composite Temperature Threshold (WCTEMP)
        /// </summary>
        public ushort WCTEMP;

        /// <summary>
        /// byte 268:269 M - Critical Composite Temperature Threshold (CCTEMP)
        /// </summary>
        public ushort CCTEMP;

        /// <summary>
        /// byte 270:271 O - Maximum Time for Firmware Activation (MTFA)
        /// </summary>
        public ushort MTFA;

        /// <summary>
        /// byte 272:275 O - Host Memory Buffer Preferred Size (HMPRE)
        /// </summary>
        public uint HMPRE;

        /// <summary>
        /// byte 276:279 O - Host Memory Buffer Minimum Size (HMMIN)
        /// </summary>
        public uint HMMIN;

        /// <summary>
        /// byte 280:295 O - Total NVM Capacity (TNVMCAP)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] TNVMCAP;

        /// <summary>
        /// byte 296:311 O - Unallocated NVM Capacity (UNVMCAP)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] UNVMCAP;

        /// <summary>
        /// byte 312:315 O - Replay Protected Memory Block Support (RPMBS)
        /// </summary>
        public uint RPMBS;

        /// <summary>
        /// byte 316:511
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 196)]
        public byte[] Reserved1;

        /// <summary>
        /// byte 512 M - Submission Queue Entry Size (SQES)
        /// </summary>
        public byte SQES;

        /// <summary>
        /// byte 513 M - Completion Queue Entry Size (CQES)
        /// </summary>
        public byte CQES;

        /// <summary>
        /// byte 514:515
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Reserved2;

        /// <summary>
        /// byte 516:519 M - Number of Namespaces (NN)
        /// </summary>
        public uint NN;

        /// <summary>
        /// byte 520:521 M - Optional NVM Command Support (ONCS)
        /// </summary>
        public ushort ONCS;

        /// <summary>
        /// byte 522:523 M - Fused Operation Support (FUSES)
        /// </summary>
        public ushort FUSES;

        /// <summary>
        /// byte 524 M - Format NVM Attributes (FNA)
        /// </summary>
        public byte FNA;

        /// <summary>
        /// byte 525 M - Volatile Write Cache (VWC)
        /// </summary>
        public byte VWC;

        /// <summary>
        /// byte 526:527 M - Atomic Write Unit Normal (AWUN)
        /// </summary>
        public ushort AWUN;

        /// <summary>
        /// byte 528:529 M - Atomic Write Unit Power Fail (AWUPF)
        /// </summary>
        public ushort AWUPF;

        /// <summary>
        /// byte 530 M - NVM Vendor Specific Command Configuration (NVSCC)
        /// </summary>
        public byte NVSCC;

        /// <summary>
        /// byte 531
        /// </summary>
        public byte Reserved3;

        /// <summary>
        /// byte 532:533 O - Atomic Compare and Write Unit (ACWU)
        /// </summary>
        public ushort ACWU;

        /// <summary>
        /// byte 534:535
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Reserved4;

        /// <summary>
        /// byte 536:539 O - SGL Support (SGLS)
        /// </summary>
        public uint SGLS;

        /// <summary>
        /// byte 540:703
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 164)]
        public byte[] Reserved5;

        /// <summary>
        /// byte 704:2047
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1344)]
        public byte[] Reserved6;

        /// <summary>
        /// byte 2048:3071 Power State Descriptors
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public NVME_POWER_STATE_DESC[] PDS;

        /// <summary>
        /// byte 3072:4095 Vendor Specific
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        public byte[] VS;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NVME_HEALTH_INFO_LOG
    {
        /// <summary>
        /// This field indicates critical warnings for the state of the  controller.
        /// Each bit corresponds to a critical warning type; multiple bits may be set.
        /// </summary>
        public byte CriticalWarning;

        /// <summary>
        /// Composite Temperature:  Contains the temperature of the overall device (controller and NVM included) in units of Kelvin.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] CompositeTemp;

        /// <summary>
        /// Available Spare:  Contains a normalized percentage (0 to 100%) of the remaining spare capacity available
        /// </summary>
        public byte AvailableSpare;

        /// <summary>
        /// Available Spare Threshold:  When the Available Spare falls below the threshold indicated in this field,
        /// an asynchronous event completion may occur. The value is indicated as a normalized percentage (0 to 100%).
        /// </summary>
        public byte AvailableSpareThreshold;

        /// <summary>
        /// Percentage Used:  Contains a vendor specific estimate of the percentage of NVM subsystem life used based on
        /// the actual usage and the manufacturer’s prediction of NVM life. A value of 100 indicates that the estimated endurance of
        /// the NVM in the NVM subsystem has been consumed, but may not indicate an NVM subsystem failure. The value is allowed to exceed 100.
        /// </summary>
        public byte PercentageUsed;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
        public byte[] Reserved1;

        /// <summary>
        /// Data Units Read:  Contains the number of 512 byte data units the host has read from the controller;
        /// this value does not include metadata. This value is reported in thousands
        /// (i.e., a value of 1 corresponds to 1000 units of 512 bytes read) and is rounded up.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] DataUnitRead;

        /// <summary>
        /// Data Units Written:  Contains the number of 512 byte data units the host has written to the controller;
        /// this value does not include metadata. This value is reported in thousands
        /// (i.e., a value of 1 corresponds to 1000 units of 512 bytes written) and is rounded up.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] DataUnitWritten;

        /// <summary>
        /// Host Read Commands:  Contains the number of read commands completed by the controller.
        /// For the NVM command set, this is the number of Compare and Read commands.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] HostReadCommands;

        /// <summary>
        /// Host Write Commands:  Contains the number of write commands completed by the controller.
        /// For the NVM command set, this is the number of Write commands.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] HostWriteCommands;

        /// <summary>
        /// Controller Busy Time:  Contains the amount of time the controller is busy with I/O commands.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ControllerBusyTime;

        /// <summary>
        /// Power Cycles:  Contains the number of power cycles.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] PowerCycles;

        /// <summary>
        /// Power On Hours:  Contains the number of power-on hours.
        /// This does not include time that the controller was powered and in a low power state condition.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] PowerOnHours;

        /// <summary>
        /// Unsafe Shutdowns:  Contains the number of unsafe shutdowns.
        /// This count is incremented when a shutdown notification is not received prior to loss of power.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] UnsafeShutdowns;

        /// <summary>
        /// Media Errors:  Contains the number of occurrences where the controller detected an unrecoverable data integrity error.
        /// Errors such as uncorrectable ECC, CRC checksum failure, or LBA tag mismatch are included in this field.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] MediaAndDataIntegrityErrors;

        /// <summary>
        /// Number of Error Information Log Entries:  Contains the number of Error Information log entries over the life of the controller
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] NumberErrorInformationLogEntries;

        /// <summary>
        /// Warning Composite Temperature Time:  Contains the amount of time in minutes that the controller is operational and the Composite Temperature is greater than or equal to the Warning Composite
        /// Temperature Threshold.
        /// </summary>
        public uint WarningCompositeTemperatureTime;

        /// <summary>
        /// Critical Composite Temperature Time:  Contains the amount of time in minutes that the controller is operational and the Composite Temperature is greater than the Critical Composite Temperature
        /// Threshold.
        /// </summary>
        public uint CriticalCompositeTemperatureTime;

        /// <summary>
        /// Contains the current temperature reported by temperature sensor 1-8.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public ushort[] TemperatureSensor;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 296)]
        internal byte[] Reserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_PROTOCOL_SPECIFIC_DATA
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
    internal struct STORAGE_QUERY_BUFFER
    {
        public STORAGE_PROPERTY_ID PropertyId;
        public STORAGE_QUERY_TYPE QueryType;
        public STORAGE_PROTOCOL_SPECIFIC_DATA ProtocolSpecific;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
        internal byte[] Buffer;
    }

    internal enum STORAGE_PROTOCOL_NVME_PROTOCOL_DATA_REQUEST_VALUE
    {
        NVMeIdentifyCnsSpecificNamespace = 0,
        NVMeIdentifyCnsController = 1,
        NVMeIdentifyCnsActiveNamespaces = 2
    }

    internal enum STORAGE_PROTOCOL_TYPE
    {
        ProtocolTypeUnknown = 0x00,
        ProtocolTypeScsi,
        ProtocolTypeAta,
        ProtocolTypeNvme,
        ProtocolTypeSd,
        ProtocolTypeProprietary = 0x7E,
        ProtocolTypeMaxReserved = 0x7F
    }

    internal enum STORAGE_PROPERTY_ID
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
        StorageDeviceLocationProperty
    }

    internal enum STORAGE_QUERY_TYPE
    {
        PropertyStandardQuery = 0,
        PropertyExistsQuery,
        PropertyMaskQuery,
        PropertyQueryMaxDefined
    }

    internal enum IOCTL : uint
    {
        IOCTL_SCSI_PASS_THROUGH = 0x04d004,
        IOCTL_SCSI_MINIPORT = 0x04d008,
        IOCTL_SCSI_PASS_THROUGH_DIRECT = 0x04d014,
        IOCTL_SCSI_GET_ADDRESS = 0x41018,
        IOCTL_DISK_PERFORMANCE = 0x70020,
        IOCTL_STORAGE_QUERY_PROPERTY = 0x2D1400,
        IOCTL_BATTERY_QUERY_TAG = 0x294040,
        IOCTL_BATTERY_QUERY_INFORMATION = 0x294044,
        IOCTL_BATTERY_QUERY_STATUS = 0x29404C
    }

    internal enum STORAGE_BUS_TYPE
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

    internal enum STORAGE_PROTOCOL_NVME_DATA_TYPE
    {
        NVMeDataTypeUnknown = 0,
        NVMeDataTypeIdentify,
        NVMeDataTypeLogPage,
        NVMeDataTypeFeature
    }

    internal enum NVME_LOG_PAGES
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

    [Flags]
    public enum NVME_CRITICAL_WARNING
    {
        None = 0x00,

        /// <summary>
        /// If set to 1, then the available spare space has fallen below the threshold.
        /// </summary>
        AvailableSpaceLow = 0x01,

        /// <summary>
        /// If set to 1, then a temperature is above an over temperature threshold or below an under temperature threshold.
        /// </summary>
        TemperatureThreshold = 0x02,

        /// <summary>
        /// If set to 1, then the device reliability has been degraded due to significant media related errors or any internal error that degrades device reliability.
        /// </summary>
        ReliabilityDegraded = 0x04,

        /// <summary>
        /// If set to 1, then the media has been placed in read only mode
        /// </summary>
        ReadOnly = 0x08,

        /// <summary>
        /// If set to 1, then the volatile memory backup device has failed. This field is only valid if the controller has a volatile memory backup solution.
        /// </summary>
        VolatileMemoryBackupDeviceFailed = 0x10
    }
}
