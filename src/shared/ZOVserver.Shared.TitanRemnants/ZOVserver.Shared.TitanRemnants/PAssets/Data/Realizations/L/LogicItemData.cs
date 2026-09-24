using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (18) at 1778788288 (f28bb05e-3946-453b-9b7d-a39f4bb72070).
public class LogicItemData(CsvElement csvElement) : LogicData(csvElement)
{
    public string ParentItemForSkin { get; } = csvElement.GetStringValue("ParentItemForSkin");
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public string ExportName { get; } = csvElement.GetStringValue("ExportName");
    public string ExportNameEnemy { get; } = csvElement.GetStringValue("ExportNameEnemy");
    public string ShadowExportName { get; } = csvElement.GetStringValue("ShadowExportName");
    public string GroundGlowExportName { get; } = csvElement.GetStringValue("GroundGlowExportName");
    public string LoopingEffect { get; } = csvElement.GetStringValue("LoopingEffect");
    public int Value { get; } = csvElement.GetIntValue("Value");
    public int Value2 { get; } = csvElement.GetIntValue("Value2");
    public int TriggerRangeSubTiles { get; } = csvElement.GetIntValue("TriggerRangeSubTiles");
    public string TriggerAreaEffect { get; } = csvElement.GetStringValue("TriggerAreaEffect");
    public bool CanBePickedUp { get; } = csvElement.GetBoolValue("CanBePickedUp");
    public string SpawnEffect { get; } = csvElement.GetStringValue("SpawnEffect");
    public string Scw { get; } = csvElement.GetStringValue("SCW");
    public string ScwEnemy { get; } = csvElement.GetStringValue("SCWEnemy");
    public string Layer { get; } = csvElement.GetStringValue("Layer");

    public override string ToString()
    {
        return $"""
                LogicItemData =>
                   Name = {Name},
                   ParentItemForSkin = {ParentItemForSkin},
                   FileName = {FileName},
                   ExportName = {ExportName},
                   ExportNameEnemy = {ExportNameEnemy},
                   ShadowExportName = {ShadowExportName},
                   GroundGlowExportName = {GroundGlowExportName},
                   LoopingEffect = {LoopingEffect},
                   Value = {Value},
                   Value2 = {Value2},
                   TriggerRangeSubTiles = {TriggerRangeSubTiles},
                   TriggerAreaEffect = {TriggerAreaEffect},
                   CanBePickedUp = {CanBePickedUp},
                   SpawnEffect = {SpawnEffect},
                   SCW = {Scw},
                   SCWEnemy = {ScwEnemy},
                   Layer = {Layer}
                """;
    }
}