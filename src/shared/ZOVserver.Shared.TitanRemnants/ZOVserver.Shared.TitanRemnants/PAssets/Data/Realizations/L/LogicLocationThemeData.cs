using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (47) at 1778788288 (45afe809-e9a8-4e2c-b63c-f69f4e7de38c).
public class LogicLocationThemeData(CsvElement csvElement) : LogicData(csvElement)
{
    public string Blocking1Scw { get; } = csvElement.GetStringValue("Blocking1SCW");
    public string Blocking1Mesh { get; } = csvElement.GetStringValue("Blocking1Mesh");
    public int Blocking1AngleStep { get; } = csvElement.GetIntValue("Blocking1AngleStep");
    public string Blocking2Scw { get; } = csvElement.GetStringValue("Blocking2SCW");
    public string Blocking2Mesh { get; } = csvElement.GetStringValue("Blocking2Mesh");
    public int Blocking2AngleStep { get; } = csvElement.GetIntValue("Blocking2AngleStep");
    public string Blocking3Scw { get; } = csvElement.GetStringValue("Blocking3SCW");
    public string Blocking3Mesh { get; } = csvElement.GetStringValue("Blocking3Mesh");
    public int Blocking3AngleStep { get; } = csvElement.GetIntValue("Blocking3AngleStep");
    public string Blocking4Scw { get; } = csvElement.GetStringValue("Blocking4SCW");
    public string Blocking4Mesh { get; } = csvElement.GetStringValue("Blocking4Mesh");
    public int Blocking4AngleStep { get; } = csvElement.GetIntValue("Blocking4AngleStep");
    public string RespawningWallScw { get; } = csvElement.GetStringValue("RespawningWallSCW");
    public string RespawningWallMesh { get; } = csvElement.GetStringValue("RespawningWallMesh");
    public int RespawningWallAngleStep { get; } = csvElement.GetIntValue("RespawningWallAngleStep");
    public string RespawningForestScw { get; } = csvElement.GetStringValue("RespawningForestSCW");
    public string ForestScw { get; } = csvElement.GetStringValue("ForestSCW");
    public string DestructableScw { get; } = csvElement.GetStringValue("DestructableSCW");
    public string DestructableMesh { get; } = csvElement.GetStringValue("DestructableMesh");
    public int DestructableAngleStep { get; } = csvElement.GetIntValue("DestructableAngleStep");
    public string FragileScw { get; } = csvElement.GetStringValue("FragileSCW");
    public string FragileMesh { get; } = csvElement.GetStringValue("FragileMesh");
    public int FragileAngleStep { get; } = csvElement.GetIntValue("FragileAngleStep");
    public string WaterTileScw { get; } = csvElement.GetStringValue("WaterTileSCW");
    public string FenceScw { get; } = csvElement.GetStringValue("FenceSCW");
    public string IndestructibleScw { get; } = csvElement.GetStringValue("IndestructibleSCW");
    public string IndestructibleMesh { get; } = csvElement.GetStringValue("IndestructibleMesh");
    public string BenchScw { get; } = csvElement.GetStringValue("BenchSCW");
    public string LaserBallSkinOverride { get; } = csvElement.GetStringValue("LaserBallSkinOverride");
    public string MineGemSpawnScwOverride { get; } = csvElement.GetStringValue("MineGemSpawnSCWOverride");
    public string LootBoxSkinOverride { get; } = csvElement.GetStringValue("LootBoxSkinOverride");
    public string ShowdownBoostScwOverride { get; } = csvElement.GetStringValue("ShowdownBoostSCWOverride");
    public int MapPreviewBGColorRed { get; } = csvElement.GetIntValue("MapPreviewBGColorRed");
    public int MapPreviewBGColorGreen { get; } = csvElement.GetIntValue("MapPreviewBGColorGreen");
    public int MapPreviewBGColorBlue { get; } = csvElement.GetIntValue("MapPreviewBGColorBlue");

    public string MapPreviewGemGrabSpawnHoleExportName { get; } =
        csvElement.GetStringValue("MapPreviewGemGrabSpawnHoleExportName");

    public override string ToString()
    {
        return $"""
                LogicLocationThemeData =>
                   Name = {Name},
                   Blocking1SCW = {Blocking1Scw},
                   Blocking1Mesh = {Blocking1Mesh},
                   Blocking1AngleStep = {Blocking1AngleStep},
                   Blocking2SCW = {Blocking2Scw},
                   Blocking2Mesh = {Blocking2Mesh},
                   Blocking2AngleStep = {Blocking2AngleStep},
                   Blocking3SCW = {Blocking3Scw},
                   Blocking3Mesh = {Blocking3Mesh},
                   Blocking3AngleStep = {Blocking3AngleStep},
                   Blocking4SCW = {Blocking4Scw},
                   Blocking4Mesh = {Blocking4Mesh},
                   Blocking4AngleStep = {Blocking4AngleStep},
                   RespawningWallSCW = {RespawningWallScw},
                   RespawningWallMesh = {RespawningWallMesh},
                   RespawningWallAngleStep = {RespawningWallAngleStep},
                   RespawningForestSCW = {RespawningForestScw},
                   ForestSCW = {ForestScw},
                   DestructableSCW = {DestructableScw},
                   DestructableMesh = {DestructableMesh},
                   DestructableAngleStep = {DestructableAngleStep},
                   FragileSCW = {FragileScw},
                   FragileMesh = {FragileMesh},
                   FragileAngleStep = {FragileAngleStep},
                   WaterTileSCW = {WaterTileScw},
                   FenceSCW = {FenceScw},
                   IndestructibleSCW = {IndestructibleScw},
                   IndestructibleMesh = {IndestructibleMesh},
                   BenchSCW = {BenchScw},
                   LaserBallSkinOverride = {LaserBallSkinOverride},
                   MineGemSpawnSCWOverride = {MineGemSpawnScwOverride},
                   LootBoxSkinOverride = {LootBoxSkinOverride},
                   ShowdownBoostSCWOverride = {ShowdownBoostScwOverride},
                   MapPreviewBGColorRed = {MapPreviewBGColorRed},
                   MapPreviewBGColorGreen = {MapPreviewBGColorGreen},
                   MapPreviewBGColorBlue = {MapPreviewBGColorBlue},
                   MapPreviewGemGrabSpawnHoleExportName = {MapPreviewGemGrabSpawnHoleExportName}
                """;
    }
}