using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (10) at 1778788272 (6374bd8d-ef90-472b-ad2b-185d399aff24).
public class LogicParticleEmitterData(CsvElement csvElement) : LogicData(csvElement)
{
    public int MaxParticleCount { get; } = csvElement.GetIntValue("MaxParticleCount");
    public int MinLife { get; } = csvElement.GetIntValue("MinLife");
    public int MaxLife { get; } = csvElement.GetIntValue("MaxLife");
    public int ParticleMinInterval { get; } = csvElement.GetIntValue("ParticleMinInterval");
    public int ParticleMaxInterval { get; } = csvElement.GetIntValue("ParticleMaxInterval");
    public int ParticleMinLife { get; } = csvElement.GetIntValue("ParticleMinLife");
    public int ParticleMaxLife { get; } = csvElement.GetIntValue("ParticleMaxLife");
    public int ParticleMinAngle { get; } = csvElement.GetIntValue("ParticleMinAngle");
    public int ParticleMaxAngle { get; } = csvElement.GetIntValue("ParticleMaxAngle");
    public bool ParticleAngleRelativeToParent { get; } = csvElement.GetBoolValue("ParticleAngleRelativeToParent");
    public bool ParticleRandomAngle { get; } = csvElement.GetBoolValue("ParticleRandomAngle");
    public int ParticleMinRadius { get; } = csvElement.GetIntValue("ParticleMinRadius");
    public int ParticleMaxRadius { get; } = csvElement.GetIntValue("ParticleMaxRadius");
    public int ParticleMinSpeed { get; } = csvElement.GetIntValue("ParticleMinSpeed");
    public int ParticleMaxSpeed { get; } = csvElement.GetIntValue("ParticleMaxSpeed");
    public int ParticleStartZ { get; } = csvElement.GetIntValue("ParticleStartZ");
    public int ParticleMinVelocityZ { get; } = csvElement.GetIntValue("ParticleMinVelocityZ");
    public int ParticleMaxVelocityZ { get; } = csvElement.GetIntValue("ParticleMaxVelocityZ");
    public int ParticleGravity { get; } = csvElement.GetIntValue("ParticleGravity");
    public int ParticleMinTailLength { get; } = csvElement.GetIntValue("ParticleMinTailLength");
    public int ParticleMaxTailLength { get; } = csvElement.GetIntValue("ParticleMaxTailLength");
    public string ParticleResource { get; } = csvElement.GetStringValue("ParticleResource");
    public string ParticleExportName { get; } = csvElement.GetStringValue("ParticleExportName");
    public bool RotateToDirection { get; } = csvElement.GetBoolValue("RotateToDirection");
    public bool LoopParticleClip { get; } = csvElement.GetBoolValue("LoopParticleClip");
    public int StartScale { get; } = csvElement.GetIntValue("StartScale");
    public int EndScale { get; } = csvElement.GetIntValue("EndScale");
    public int FadeOutDuration { get; } = csvElement.GetIntValue("FadeOutDuration");
    public int Inertia { get; } = csvElement.GetIntValue("Inertia");
    public string EnemyVersion { get; } = csvElement.GetStringValue("EnemyVersion");

    public override string ToString()
    {
        return $"""
                LogicParticleEmitterData =>
                   Name = {Name},
                   MaxParticleCount = {MaxParticleCount},
                   MinLife = {MinLife},
                   MaxLife = {MaxLife},
                   ParticleMinInterval = {ParticleMinInterval},
                   ParticleMaxInterval = {ParticleMaxInterval},
                   ParticleMinLife = {ParticleMinLife},
                   ParticleMaxLife = {ParticleMaxLife},
                   ParticleMinAngle = {ParticleMinAngle},
                   ParticleMaxAngle = {ParticleMaxAngle},
                   ParticleAngleRelativeToParent = {ParticleAngleRelativeToParent},
                   ParticleRandomAngle = {ParticleRandomAngle},
                   ParticleMinRadius = {ParticleMinRadius},
                   ParticleMaxRadius = {ParticleMaxRadius},
                   ParticleMinSpeed = {ParticleMinSpeed},
                   ParticleMaxSpeed = {ParticleMaxSpeed},
                   ParticleStartZ = {ParticleStartZ},
                   ParticleMinVelocityZ = {ParticleMinVelocityZ},
                   ParticleMaxVelocityZ = {ParticleMaxVelocityZ},
                   ParticleGravity = {ParticleGravity},
                   ParticleMinTailLength = {ParticleMinTailLength},
                   ParticleMaxTailLength = {ParticleMaxTailLength},
                   ParticleResource = {ParticleResource},
                   ParticleExportName = {ParticleExportName},
                   RotateToDirection = {RotateToDirection},
                   LoopParticleClip = {LoopParticleClip},
                   StartScale = {StartScale},
                   EndScale = {EndScale},
                   FadeOutDuration = {FadeOutDuration},
                   Inertia = {Inertia},
                   EnemyVersion = {EnemyVersion}
                """;
    }
}