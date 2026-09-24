using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (27) at 1778788289 (c60dc5d2-0aea-4236-b3f3-fe27c09d0277).
public class LogicTileData(CsvElement csvElement) : LogicData(csvElement)
{
    public string TileCode { get; } = csvElement.GetStringValue("TileCode");
    public bool BlocksMovement { get; } = csvElement.GetBoolValue("BlocksMovement");
    public bool BlocksProjectiles { get; } = csvElement.GetBoolValue("BlocksProjectiles");
    public bool IsDestructible { get; } = csvElement.GetBoolValue("IsDestructible");
    public bool IsDestructibleNormalWeapon { get; } = csvElement.GetBoolValue("IsDestructibleNormalWeapon");
    public bool HidesHero { get; } = csvElement.GetBoolValue("HidesHero");
    public int RespawnSeconds { get; } = csvElement.GetIntValue("RespawnSeconds");
    public int CollisionMargin { get; } = csvElement.GetIntValue("CollisionMargin");
    public string BaseExportName { get; } = csvElement.GetStringValue("BaseExportName");
    public string BaseExplosionEffect { get; } = csvElement.GetStringValue("BaseExplosionEffect");
    public string BaseHitEffect { get; } = csvElement.GetStringValue("BaseHitEffect");
    public string BaseWindEffect { get; } = csvElement.GetStringValue("BaseWindEffect");
    public string BaseBulletHole1 { get; } = csvElement.GetStringValue("BaseBulletHole1");
    public string BaseBulletHole2 { get; } = csvElement.GetStringValue("BaseBulletHole2");
    public string BaseCrack1 { get; } = csvElement.GetStringValue("BaseCrack1");
    public string BaseCrack2 { get; } = csvElement.GetStringValue("BaseCrack2");
    public int SortOffset { get; } = csvElement.GetIntValue("SortOffset");
    public bool HasHitAnim { get; } = csvElement.GetBoolValue("HasHitAnim");
    public bool HasWindAnim { get; } = csvElement.GetBoolValue("HasWindAnim");
    public int ShadowScaleX { get; } = csvElement.GetIntValue("ShadowScaleX");
    public int ShadowScaleY { get; } = csvElement.GetIntValue("ShadowScaleY");
    public int ShadowX { get; } = csvElement.GetIntValue("ShadowX");
    public int ShadowY { get; } = csvElement.GetIntValue("ShadowY");
    public int ShadowSkew { get; } = csvElement.GetIntValue("ShadowSkew");

    public override string ToString()
    {
        return $"""
                LogicTileData =>
                   Name = {Name},
                   TileCode = {TileCode},
                   BlocksMovement = {BlocksMovement},
                   BlocksProjectiles = {BlocksProjectiles},
                   IsDestructible = {IsDestructible},
                   IsDestructibleNormalWeapon = {IsDestructibleNormalWeapon},
                   HidesHero = {HidesHero},
                   RespawnSeconds = {RespawnSeconds},
                   CollisionMargin = {CollisionMargin},
                   BaseExportName = {BaseExportName},
                   BaseExplosionEffect = {BaseExplosionEffect},
                   BaseHitEffect = {BaseHitEffect},
                   BaseWindEffect = {BaseWindEffect},
                   BaseBulletHole1 = {BaseBulletHole1},
                   BaseBulletHole2 = {BaseBulletHole2},
                   BaseCrack1 = {BaseCrack1},
                   BaseCrack2 = {BaseCrack2},
                   SortOffset = {SortOffset},
                   HasHitAnim = {HasHitAnim},
                   HasWindAnim = {HasWindAnim},
                   ShadowScaleX = {ShadowScaleX},
                   ShadowScaleY = {ShadowScaleY},
                   ShadowX = {ShadowX},
                   ShadowY = {ShadowY},
                   ShadowSkew = {ShadowSkew}
                """;
    }
}