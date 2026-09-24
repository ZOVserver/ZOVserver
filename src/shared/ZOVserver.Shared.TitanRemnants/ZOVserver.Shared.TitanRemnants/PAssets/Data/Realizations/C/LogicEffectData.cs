using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (7) at 1778788272 (ba9d694d-45ea-4beb-8efd-cf5780f80056).
public class LogicEffectData(CsvElement csvElement) : LogicData(csvElement)
{
    public bool Loop { get; } = csvElement.GetBoolValue("Loop");
    public bool FollowParent { get; } = csvElement.GetBoolValue("FollowParent");
    public bool FollowParentAngle { get; } = csvElement.GetBoolValue("FollowParentAngle");
    public string FollowBone { get; } = csvElement.GetStringValue("FollowBone");
    public int OwnScreenShake { get; } = csvElement.GetIntValue("OwnScreenShake");
    public int OthersScreenShake { get; } = csvElement.GetIntValue("OthersScreenShake");
    public int Time { get; } = csvElement.GetIntValue("Time");
    public string Sound { get; } = csvElement.GetStringValue("Sound");
    public string TypeInCsv { get; } = csvElement.GetStringValue("Type");
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public string ExportName { get; } = csvElement.GetStringValue("ExportName");
    public string ParticleEmitterName { get; } = csvElement.GetStringValue("ParticleEmitterName");
    public string Effect { get; } = csvElement.GetStringValue("Effect");
    public string Layer { get; } = csvElement.GetStringValue("Layer");
    public bool GroundBasis { get; } = csvElement.GetBoolValue("GroundBasis");
    public int FlashColor { get; } = csvElement.GetIntValue("FlashColor");
    public int Scale { get; } = csvElement.GetIntValue("Scale");
    public int FlashDuration { get; } = csvElement.GetIntValue("FlashDuration");
    public string TextInstanceName { get; } = csvElement.GetStringValue("TextInstanceName");
    public string TextParentInstanceName { get; } = csvElement.GetStringValue("TextParentInstanceName");
    public string EnemyVersion { get; } = csvElement.GetStringValue("EnemyVersion");
    public int FlashWidth { get; } = csvElement.GetIntValue("FlashWidth");
    public int EffectZ { get; } = csvElement.GetIntValue("EffectZ");

    public override string ToString()
    {
        return $"""
                LogicEffectData =>
                   Name = {Name},
                   Loop = {Loop},
                   FollowParent = {FollowParent},
                   FollowParentAngle = {FollowParentAngle},
                   FollowBone = {FollowBone},
                   OwnScreenShake = {OwnScreenShake},
                   OthersScreenShake = {OthersScreenShake},
                   Time = {Time},
                   Sound = {Sound},
                   Type = {TypeInCsv},
                   FileName = {FileName},
                   ExportName = {ExportName},
                   ParticleEmitterName = {ParticleEmitterName},
                   Effect = {Effect},
                   Layer = {Layer},
                   GroundBasis = {GroundBasis},
                   FlashColor = {FlashColor},
                   Scale = {Scale},
                   FlashDuration = {FlashDuration},
                   TextInstanceName = {TextInstanceName},
                   TextParentInstanceName = {TextParentInstanceName},
                   EnemyVersion = {EnemyVersion},
                   FlashWidth = {FlashWidth},
                   EffectZ = {EffectZ}
                """;
    }
}