using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (17) at 1778788272 (92365495-e632-426b-87ab-68a6050345db).
public class LogicAreaEffectData(CsvElement csvElement) : LogicData(csvElement)
{
    public string ParentAreaEffectForSkin { get; } = csvElement.GetStringValue("ParentAreaEffectForSkin");
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public string OwnExportName { get; } = csvElement.GetStringValue("OwnExportName");
    public string BlueExportName { get; } = csvElement.GetStringValue("BlueExportName");
    public string RedExportName { get; } = csvElement.GetStringValue("RedExportName");
    public string Layer { get; } = csvElement.GetStringValue("Layer");
    public string ExportNameTop { get; } = csvElement.GetStringValue("ExportNameTop");
    public string ExportNameObject { get; } = csvElement.GetStringValue("ExportNameObject");
    public string Effect { get; } = csvElement.GetStringValue("Effect");
    public string LoopingEffect { get; } = csvElement.GetStringValue("LoopingEffect");
    public int Scale { get; } = csvElement.GetIntValue("Scale");
    public int TimeMs { get; } = csvElement.GetIntValue("TimeMs");
    public int Radius { get; } = csvElement.GetIntValue("Radius");
    public int Damage { get; } = csvElement.GetIntValue("Damage");
    public int CustomValue { get; } = csvElement.GetIntValue("CustomValue");
    public string TypeInCsv { get; } = csvElement.GetStringValue("Type");
    public string BulletExplosionBullet { get; } = csvElement.GetStringValue("BulletExplosionBullet");
    public int BulletExplosionBulletDistance { get; } = csvElement.GetIntValue("BulletExplosionBulletDistance");
    public string BulletExplosionItem { get; } = csvElement.GetStringValue("BulletExplosionItem");
    public bool DestroysEnvironment { get; } = csvElement.GetBoolValue("DestroysEnvironment");
    public int PushbackStrength { get; } = csvElement.GetIntValue("PushbackStrength");
    public int PushbackStrengthSelf { get; } = csvElement.GetIntValue("PushbackStrengthSelf");
    public int FreezeStrength { get; } = csvElement.GetIntValue("FreezeStrength");
    public bool ShouldShowEvenIfOutsideScreen { get; } = csvElement.GetBoolValue("ShouldShowEvenIfOutsideScreen");
    public int SameAreaEffectCanNotDamageMs { get; } = csvElement.GetIntValue("SameAreaEffectCanNotDamageMs");
    public bool DontShowToEnemy { get; } = csvElement.GetBoolValue("DontShowToEnemy");

    public override string ToString()
    {
        return $"""
                LogicAreaEffectData =>
                   Name = {Name},
                   ParentAreaEffectForSkin = {ParentAreaEffectForSkin},
                   FileName = {FileName},
                   OwnExportName = {OwnExportName},
                   BlueExportName = {BlueExportName},
                   RedExportName = {RedExportName},
                   Layer = {Layer},
                   ExportNameTop = {ExportNameTop},
                   ExportNameObject = {ExportNameObject},
                   Effect = {Effect},
                   LoopingEffect = {LoopingEffect},
                   Scale = {Scale},
                   TimeMs = {TimeMs},
                   Radius = {Radius},
                   Damage = {Damage},
                   CustomValue = {CustomValue},
                   Type = {TypeInCsv},
                   BulletExplosionBullet = {BulletExplosionBullet},
                   BulletExplosionBulletDistance = {BulletExplosionBulletDistance},
                   BulletExplosionItem = {BulletExplosionItem},
                   DestroysEnvironment = {DestroysEnvironment},
                   PushbackStrength = {PushbackStrength},
                   PushbackStrengthSelf = {PushbackStrengthSelf},
                   FreezeStrength = {FreezeStrength},
                   ShouldShowEvenIfOutsideScreen = {ShouldShowEvenIfOutsideScreen},
                   SameAreaEffectCanNotDamageMs = {SameAreaEffectCanNotDamageMs},
                   DontShowToEnemy = {DontShowToEnemy}
                """;
    }
}