using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (23) at 1778788288 (b856aa50-fe43-4c8d-8ddd-3f3f55599b61).
public class LogicCardData(CsvElement csvElement) : LogicData(csvElement)
{
    public string IconSwf { get; } = csvElement.GetStringValue("IconSWF");
    public string IconExportName { get; } = csvElement.GetStringValue("IconExportName");
    public string Target { get; } = csvElement.GetStringValue("Target");
    public bool LockedForChronos { get; } = csvElement.GetBoolValue("LockedForChronos");
    public int MetaType { get; } = csvElement.GetIntValue("MetaType");
    public string RequiresCard { get; } = csvElement.GetStringValue("RequiresCard");
    public string TypeInCsv { get; } = csvElement.GetStringValue("Type");
    public string Skill { get; } = csvElement.GetStringValue("Skill");
    public int Value { get; } = csvElement.GetIntValue("Value");
    public int Value2 { get; } = csvElement.GetIntValue("Value2");
    public int Value3 { get; } = csvElement.GetIntValue("Value3");
    public string Rarity { get; } = csvElement.GetStringValue("Rarity");
    public string Tid { get; } = csvElement.GetStringValue("TID");
    public string PowerNumberTid { get; } = csvElement.GetStringValue("PowerNumberTID");
    public string PowerNumber2Tid { get; } = csvElement.GetStringValue("PowerNumber2TID");
    public string PowerIcon1ExportName { get; } = csvElement.GetStringValue("PowerIcon1ExportName");
    public string PowerIcon2ExportName { get; } = csvElement.GetStringValue("PowerIcon2ExportName");
    public int SortOrder { get; } = csvElement.GetIntValue("SortOrder");
    public bool DontUpgradeStat { get; } = csvElement.GetBoolValue("DontUpgradeStat");

    public override string ToString()
    {
        return $"""
                LogicCardData =>
                   Name = {Name},
                   IconSWF = {IconSwf},
                   IconExportName = {IconExportName},
                   Target = {Target},
                   LockedForChronos = {LockedForChronos},
                   MetaType = {MetaType},
                   RequiresCard = {RequiresCard},
                   Type = {TypeInCsv},
                   Skill = {Skill},
                   Value = {Value},
                   Value2 = {Value2},
                   Value3 = {Value3},
                   Rarity = {Rarity},
                   TID = {Tid},
                   PowerNumberTID = {PowerNumberTid},
                   PowerNumber2TID = {PowerNumber2Tid},
                   PowerIcon1ExportName = {PowerIcon1ExportName},
                   PowerIcon2ExportName = {PowerIcon2ExportName},
                   SortOrder = {SortOrder},
                   DontUpgradeStat = {DontUpgradeStat}
                """;
    }
}