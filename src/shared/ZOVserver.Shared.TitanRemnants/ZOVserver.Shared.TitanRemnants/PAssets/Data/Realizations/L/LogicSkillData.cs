using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (20) at 1778788288 (7772893d-e459-4419-a13a-5f5e78c2cc00).
public class LogicSkillData(CsvElement csvElement) : LogicData(csvElement)
{
    public string BehaviorType { get; } = csvElement.GetStringValue("BehaviorType");
    public bool CanMoveAtSameTime { get; } = csvElement.GetBoolValue("CanMoveAtSameTime");
    public bool Targeted { get; } = csvElement.GetBoolValue("Targeted");
    public bool CanAutoShoot { get; } = csvElement.GetBoolValue("CanAutoShoot");
    public int Cooldown { get; } = csvElement.GetIntValue("Cooldown");
    public int ActiveTime { get; } = csvElement.GetIntValue("ActiveTime");
    public int CastingTime { get; } = csvElement.GetIntValue("CastingTime");
    public int CastingRange { get; } = csvElement.GetIntValue("CastingRange");
    public int RangeVisual { get; } = csvElement.GetIntValue("RangeVisual");
    public int MaxCastingRange { get; } = csvElement.GetIntValue("MaxCastingRange");
    public int RechargeTime { get; } = csvElement.GetIntValue("RechargeTime");
    public int MaxCharge { get; } = csvElement.GetIntValue("MaxCharge");
    public int Damage { get; } = csvElement.GetIntValue("Damage");
    public int MsBetweenAttacks { get; } = csvElement.GetIntValue("MsBetweenAttacks");
    public int Spread { get; } = csvElement.GetIntValue("Spread");
    public int AttackPattern { get; } = csvElement.GetIntValue("AttackPattern");
    public int NumBulletsInOneAttack { get; } = csvElement.GetIntValue("NumBulletsInOneAttack");
    public bool TwoGuns { get; } = csvElement.GetBoolValue("TwoGuns");
    public bool ExecuteFirstAttackImmediately { get; } = csvElement.GetBoolValue("ExecuteFirstAttackImmediately");
    public int ChargePushback { get; } = csvElement.GetIntValue("ChargePushback");
    public int ChargeSpeed { get; } = csvElement.GetIntValue("ChargeSpeed");
    public int ChargeType { get; } = csvElement.GetIntValue("ChargeType");
    public int NumSpawns { get; } = csvElement.GetIntValue("NumSpawns");
    public int MaxSpawns { get; } = csvElement.GetIntValue("MaxSpawns");
    public bool BreakInvisibilityOnAttack { get; } = csvElement.GetBoolValue("BreakInvisibilityOnAttack");
    public int SeeInvisibilityDistance { get; } = csvElement.GetIntValue("SeeInvisibilityDistance");
    public bool AlwaysCastAtMaxRange { get; } = csvElement.GetBoolValue("AlwaysCastAtMaxRange");
    public string Projectile { get; } = csvElement.GetStringValue("Projectile");
    public string SummonedCharacter { get; } = csvElement.GetStringValue("SummonedCharacter");
    public string AreaEffectObject { get; } = csvElement.GetStringValue("AreaEffectObject");
    public string AreaEffectObject2 { get; } = csvElement.GetStringValue("AreaEffectObject2");
    public string SpawnedItem { get; } = csvElement.GetStringValue("SpawnedItem");
    public string IconSwf { get; } = csvElement.GetStringValue("IconSWF");
    public string IconExportName { get; } = csvElement.GetStringValue("IconExportName");
    public string LargeIconSwf { get; } = csvElement.GetStringValue("LargeIconSWF");
    public string LargeIconExportName { get; } = csvElement.GetStringValue("LargeIconExportName");
    public string ButtonSwf { get; } = csvElement.GetStringValue("ButtonSWF");
    public string ButtonExportName { get; } = csvElement.GetStringValue("ButtonExportName");
    public string AttackEffect { get; } = csvElement.GetStringValue("AttackEffect");
    public string UseEffect { get; } = csvElement.GetStringValue("UseEffect");
    public string EndEffect { get; } = csvElement.GetStringValue("EndEffect");
    public string LoopEffect { get; } = csvElement.GetStringValue("LoopEffect");
    public string LoopEffect2 { get; } = csvElement.GetStringValue("LoopEffect2");
    public string ChargeMoveSound { get; } = csvElement.GetStringValue("ChargeMoveSound");
    public bool MultiShot { get; } = csvElement.GetBoolValue("MultiShot");
    public bool SkillCanChange { get; } = csvElement.GetBoolValue("SkillCanChange");
    public bool ShowTimerBar { get; } = csvElement.GetBoolValue("ShowTimerBar");
    public string SecondaryProjectile { get; } = csvElement.GetStringValue("SecondaryProjectile");
    public int ChargedShotCount { get; } = csvElement.GetIntValue("ChargedShotCount");
    public int DamageModifier { get; } = csvElement.GetIntValue("DamageModifier");

    public override string ToString()
    {
        return $"""
                LogicSkillData =>
                   Name = {Name},
                   BehaviorType = {BehaviorType},
                   CanMoveAtSameTime = {CanMoveAtSameTime},
                   Targeted = {Targeted},
                   CanAutoShoot = {CanAutoShoot},
                   Cooldown = {Cooldown},
                   ActiveTime = {ActiveTime},
                   CastingTime = {CastingTime},
                   CastingRange = {CastingRange},
                   RangeVisual = {RangeVisual},
                   MaxCastingRange = {MaxCastingRange},
                   RechargeTime = {RechargeTime},
                   MaxCharge = {MaxCharge},
                   Damage = {Damage},
                   MsBetweenAttacks = {MsBetweenAttacks},
                   Spread = {Spread},
                   AttackPattern = {AttackPattern},
                   NumBulletsInOneAttack = {NumBulletsInOneAttack},
                   TwoGuns = {TwoGuns},
                   ExecuteFirstAttackImmediately = {ExecuteFirstAttackImmediately},
                   ChargePushback = {ChargePushback},
                   ChargeSpeed = {ChargeSpeed},
                   ChargeType = {ChargeType},
                   NumSpawns = {NumSpawns},
                   MaxSpawns = {MaxSpawns},
                   BreakInvisibilityOnAttack = {BreakInvisibilityOnAttack},
                   SeeInvisibilityDistance = {SeeInvisibilityDistance},
                   AlwaysCastAtMaxRange = {AlwaysCastAtMaxRange},
                   Projectile = {Projectile},
                   SummonedCharacter = {SummonedCharacter},
                   AreaEffectObject = {AreaEffectObject},
                   AreaEffectObject2 = {AreaEffectObject2},
                   SpawnedItem = {SpawnedItem},
                   IconSWF = {IconSwf},
                   IconExportName = {IconExportName},
                   LargeIconSWF = {LargeIconSwf},
                   LargeIconExportName = {LargeIconExportName},
                   ButtonSWF = {ButtonSwf},
                   ButtonExportName = {ButtonExportName},
                   AttackEffect = {AttackEffect},
                   UseEffect = {UseEffect},
                   EndEffect = {EndEffect},
                   LoopEffect = {LoopEffect},
                   LoopEffect2 = {LoopEffect2},
                   ChargeMoveSound = {ChargeMoveSound},
                   MultiShot = {MultiShot},
                   SkillCanChange = {SkillCanChange},
                   ShowTimerBar = {ShowTimerBar},
                   SecondaryProjectile = {SecondaryProjectile},
                   ChargedShotCount = {ChargedShotCount},
                   DamageModifier = {DamageModifier}
                """;
    }
}