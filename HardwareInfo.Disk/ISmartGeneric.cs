namespace HardwareInfo.Disk;

public interface ISmartGeneric : ISmart
{
    SmartAssessment Assessment { get; }

    IReadOnlyList<SmartId> GetSupportedIds();

    SmartAttribute? GetAttribute(SmartId id);
}
