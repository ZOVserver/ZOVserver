using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (15) at 1778788288 (821b5de8-24d1-4244-aec4-ca6b4e699403).
public class LogicLocationData(CsvElement csvElement) : LogicData(csvElement)
{
    public bool Disabled { get; } = csvElement.GetBoolValue("Disabled");
    public string Tid { get; } = csvElement.GetStringValue("TID");
    public string TileSetPrefix { get; } = csvElement.GetStringValue("TileSetPrefix");
    public string BgPrefix { get; } = csvElement.GetStringValue("BgPrefix");
    public string LocationTheme { get; } = csvElement.GetStringValue("LocationTheme");
    public string GroundScw { get; } = csvElement.GetStringValue("GroundSCW");
    public string CampaignGroundScw { get; } = csvElement.GetStringValue("CampaignGroundSCW");
    public string EnvironmentScw { get; } = csvElement.GetStringValue("EnvironmentSCW");
    public string MaskedEnvironmentScw { get; } = csvElement.GetStringValue("MaskedEnvironmentSCW");
    public int TileSetRedAdd { get; } = csvElement.GetIntValue("TileSetRedAdd");
    public int TileSetGreenAdd { get; } = csvElement.GetIntValue("TileSetGreenAdd");
    public int TileSetBlueAdd { get; } = csvElement.GetIntValue("TileSetBlueAdd");
    public int TileSetRedMul { get; } = csvElement.GetIntValue("TileSetRedMul");
    public int TileSetGreenMul { get; } = csvElement.GetIntValue("TileSetGreenMul");
    public int TileSetBlueMul { get; } = csvElement.GetIntValue("TileSetBlueMul");
    public string IconSwf { get; } = csvElement.GetStringValue("IconSWF");
    public string IconExportName { get; } = csvElement.GetStringValue("IconExportName");
    public string GameMode { get; } = csvElement.GetStringValue("GameMode");
    public string AllowedMaps { get; } = csvElement.GetStringValue("AllowedMaps");
    public int ShadowR { get; } = csvElement.GetIntValue("ShadowR");
    public int ShadowG { get; } = csvElement.GetIntValue("ShadowG");
    public int ShadowB { get; } = csvElement.GetIntValue("ShadowB");
    public int ShadowA { get; } = csvElement.GetIntValue("ShadowA");
    public string Music { get; } = csvElement.GetStringValue("Music");
    public string CommunityCredit { get; } = csvElement.GetStringValue("CommunityCredit");

    public override string ToString()
    {
        return $"""
                LogicLocationData =>
                   Name = {Name},
                   Disabled = {Disabled},
                   TID = {Tid},
                   TileSetPrefix = {TileSetPrefix},
                   BgPrefix = {BgPrefix},
                   LocationTheme = {LocationTheme},
                   GroundSCW = {GroundScw},
                   CampaignGroundSCW = {CampaignGroundScw},
                   EnvironmentSCW = {EnvironmentScw},
                   MaskedEnvironmentSCW = {MaskedEnvironmentScw},
                   TileSetRedAdd = {TileSetRedAdd},
                   TileSetGreenAdd = {TileSetGreenAdd},
                   TileSetBlueAdd = {TileSetBlueAdd},
                   TileSetRedMul = {TileSetRedMul},
                   TileSetGreenMul = {TileSetGreenMul},
                   TileSetBlueMul = {TileSetBlueMul},
                   IconSWF = {IconSwf},
                   IconExportName = {IconExportName},
                   GameMode = {GameMode},
                   AllowedMaps = {AllowedMaps},
                   ShadowR = {ShadowR},
                   ShadowG = {ShadowG},
                   ShadowB = {ShadowB},
                   ShadowA = {ShadowA},
                   Music = {Music},
                   CommunityCredit = {CommunityCredit}
                """;
    }
}