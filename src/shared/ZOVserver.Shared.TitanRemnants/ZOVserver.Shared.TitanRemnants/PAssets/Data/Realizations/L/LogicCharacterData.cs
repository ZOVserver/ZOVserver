using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (16) at 1778788288 (1d8d5da8-726f-4067-8913-b76502806cff).
public class LogicCharacterData(CsvElement csvElement) : LogicData(csvElement)
{
    public bool LockedForChronos { get; } = csvElement.GetBoolValue("LockedForChronos");
    public bool Disabled { get; } = csvElement.GetBoolValue("Disabled");
    public string ItemName { get; } = csvElement.GetStringValue("ItemName");
    public string WeaponSkill { get; } = csvElement.GetStringValue("WeaponSkill");
    public string UltimateSkill { get; } = csvElement.GetStringValue("UltimateSkill");
    public string Pet { get; } = csvElement.GetStringValue("Pet");
    public int Speed { get; } = csvElement.GetIntValue("Speed");
    public int Hitpoints { get; } = csvElement.GetIntValue("Hitpoints");
    public bool MeleeAutoAttackSplashDamage { get; } = csvElement.GetBoolValue("MeleeAutoAttackSplashDamage");
    public int AutoAttackSpeedMs { get; } = csvElement.GetIntValue("AutoAttackSpeedMs");
    public int AutoAttackDamage { get; } = csvElement.GetIntValue("AutoAttackDamage");
    public int AutoAttackBulletsPerShot { get; } = csvElement.GetIntValue("AutoAttackBulletsPerShot");
    public string AutoAttackMode { get; } = csvElement.GetStringValue("AutoAttackMode");
    public int AutoAttackProjectileSpread { get; } = csvElement.GetIntValue("AutoAttackProjectileSpread");
    public string AutoAttackProjectile { get; } = csvElement.GetStringValue("AutoAttackProjectile");
    public int AutoAttackRange { get; } = csvElement.GetIntValue("AutoAttackRange");
    public int RegeneratePerSecond { get; } = csvElement.GetIntValue("RegeneratePerSecond");
    public int UltiChargeMul { get; } = csvElement.GetIntValue("UltiChargeMul");
    public int UltiChargeUltiMul { get; } = csvElement.GetIntValue("UltiChargeUltiMul");
    public string TypeInCsv { get; } = csvElement.GetStringValue("Type");
    public int DamagerPercentFromAliens { get; } = csvElement.GetIntValue("DamagerPercentFromAliens");
    public string DefaultSkin { get; } = csvElement.GetStringValue("DefaultSkin");
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public string BlueExportName { get; } = csvElement.GetStringValue("BlueExportName");
    public string RedExportName { get; } = csvElement.GetStringValue("RedExportName");
    public string ShadowExportName { get; } = csvElement.GetStringValue("ShadowExportName");
    public string AreaEffect { get; } = csvElement.GetStringValue("AreaEffect");
    public string DeathAreaEffect { get; } = csvElement.GetStringValue("DeathAreaEffect");
    public string TakeDamageEffect { get; } = csvElement.GetStringValue("TakeDamageEffect");
    public string DeathEffect { get; } = csvElement.GetStringValue("DeathEffect");
    public string MoveEffect { get; } = csvElement.GetStringValue("MoveEffect");
    public string ReloadEffect { get; } = csvElement.GetStringValue("ReloadEffect");
    public string OutOfAmmoEffect { get; } = csvElement.GetStringValue("OutOfAmmoEffect");
    public string DryFireEffect { get; } = csvElement.GetStringValue("DryFireEffect");
    public string SpawnEffect { get; } = csvElement.GetStringValue("SpawnEffect");
    public string MeleeHitEffect { get; } = csvElement.GetStringValue("MeleeHitEffect");
    public string AutoAttackStartEffect { get; } = csvElement.GetStringValue("AutoAttackStartEffect");
    public string BoneEffect1 { get; } = csvElement.GetStringValue("BoneEffect1");
    public string BoneEffect2 { get; } = csvElement.GetStringValue("BoneEffect2");
    public string BoneEffect3 { get; } = csvElement.GetStringValue("BoneEffect3");
    public string BoneEffect4 { get; } = csvElement.GetStringValue("BoneEffect4");
    public string BoneEffectUse { get; } = csvElement.GetStringValue("BoneEffectUse");
    public string LoopedEffect { get; } = csvElement.GetStringValue("LoopedEffect");
    public string LoopedEffect2 { get; } = csvElement.GetStringValue("LoopedEffect2");
    public string KillCelebrationSoundVo { get; } = csvElement.GetStringValue("KillCelebrationSoundVO");
    public string InLeadCelebrationSoundVo { get; } = csvElement.GetStringValue("InLeadCelebrationSoundVO");
    public string StartSoundVo { get; } = csvElement.GetStringValue("StartSoundVO");
    public string UseUltiSoundVo { get; } = csvElement.GetStringValue("UseUltiSoundVO");
    public string TakeDamageSoundVo { get; } = csvElement.GetStringValue("TakeDamageSoundVO");
    public string DeathSoundVo { get; } = csvElement.GetStringValue("DeathSoundVO");
    public string AttackSoundVo { get; } = csvElement.GetStringValue("AttackSoundVO");
    public int AttackStartEffectOffset { get; } = csvElement.GetIntValue("AttackStartEffectOffset");
    public int TwoWeaponAttackEffectOffset { get; } = csvElement.GetIntValue("TwoWeaponAttackEffectOffset");
    public int ShadowScaleX { get; } = csvElement.GetIntValue("ShadowScaleX");
    public int ShadowScaleY { get; } = csvElement.GetIntValue("ShadowScaleY");
    public int ShadowX { get; } = csvElement.GetIntValue("ShadowX");
    public int ShadowY { get; } = csvElement.GetIntValue("ShadowY");
    public int ShadowSkew { get; } = csvElement.GetIntValue("ShadowSkew");
    public int Scale { get; } = csvElement.GetIntValue("Scale");
    public int HeroScreenScale { get; } = csvElement.GetIntValue("HeroScreenScale");
    public int FitToBoxScale { get; } = csvElement.GetIntValue("FitToBoxScale");
    public int EndScreenScale { get; } = csvElement.GetIntValue("EndScreenScale");
    public int GatchaScreenScale { get; } = csvElement.GetIntValue("GatchaScreenScale");
    public int HomeScreenScale { get; } = csvElement.GetIntValue("HomeScreenScale");
    public int HeroScreenXOffset { get; } = csvElement.GetIntValue("HeroScreenXOffset");
    public int HeroScreenZOffset { get; } = csvElement.GetIntValue("HeroScreenZOffset");
    public int CollisionRadius { get; } = csvElement.GetIntValue("CollisionRadius");
    public string HealthBar { get; } = csvElement.GetStringValue("HealthBar");
    public int HealthBarOffsetY { get; } = csvElement.GetIntValue("HealthBarOffsetY");
    public int FlyingHeight { get; } = csvElement.GetIntValue("FlyingHeight");
    public int ProjectileStartZ { get; } = csvElement.GetIntValue("ProjectileStartZ");
    public int StopMovementAfterMs { get; } = csvElement.GetIntValue("StopMovementAfterMS");
    public int WaitMs { get; } = csvElement.GetIntValue("WaitMS");
    public string Tid { get; } = csvElement.GetStringValue("TID");
    public string HeroBundleTid { get; } = csvElement.GetStringValue("HeroBundleTID");
    public bool ForceAttackAnimationToEnd { get; } = csvElement.GetBoolValue("ForceAttackAnimationToEnd");
    public string IconSwf { get; } = csvElement.GetStringValue("IconSWF");
    public string IconExportName { get; } = csvElement.GetStringValue("IconExportName");
    public int RecoilAmount { get; } = csvElement.GetIntValue("RecoilAmount");
    public string Homeworld { get; } = csvElement.GetStringValue("Homeworld");
    public string FootstepClip { get; } = csvElement.GetStringValue("FootstepClip");
    public int DifferentFootstepOffset { get; } = csvElement.GetIntValue("DifferentFootstepOffset");
    public int FootstepIntervalMs { get; } = csvElement.GetIntValue("FootstepIntervalMS");
    public int AttackingWeaponScale { get; } = csvElement.GetIntValue("AttackingWeaponScale");
    public bool UseThrowingLeftWeaponBoneScaling { get; } = csvElement.GetBoolValue("UseThrowingLeftWeaponBoneScaling");

    public bool UseThrowingRightWeaponBoneScaling { get; } =
        csvElement.GetBoolValue("UseThrowingRightWeaponBoneScaling");

    public int CommonSetUpgradeBonus { get; } = csvElement.GetIntValue("CommonSetUpgradeBonus");
    public int RareSetUpgradeBonus { get; } = csvElement.GetIntValue("RareSetUpgradeBonus");
    public int SuperRareSetUpgradeBonus { get; } = csvElement.GetIntValue("SuperRareSetUpgradeBonus");
    public bool CanWalkOverWater { get; } = csvElement.GetBoolValue("CanWalkOverWater");
    public bool UseColorMod { get; } = csvElement.GetBoolValue("UseColorMod");
    public int RedAdd { get; } = csvElement.GetIntValue("RedAdd");
    public int GreenAdd { get; } = csvElement.GetIntValue("GreenAdd");
    public int BlueAdd { get; } = csvElement.GetIntValue("BlueAdd");
    public int RedMul { get; } = csvElement.GetIntValue("RedMul");
    public int GreenMul { get; } = csvElement.GetIntValue("GreenMul");
    public int BlueMul { get; } = csvElement.GetIntValue("BlueMul");
    public int ChargeUltiAutomatically { get; } = csvElement.GetIntValue("ChargeUltiAutomatically");
    public string VideoLink { get; } = csvElement.GetStringValue("VideoLink");
    public bool ShouldEncodePetStatus { get; } = csvElement.GetBoolValue("ShouldEncodePetStatus");
    public bool SecondaryPet { get; } = csvElement.GetBoolValue("SecondaryPet");
    public int ExtraMinions { get; } = csvElement.GetIntValue("ExtraMinions");
    public int PetAutoSpawnDelay { get; } = csvElement.GetIntValue("PetAutoSpawnDelay");

    public bool IsDecoy()
    {
        return TypeInCsv == "Minion_Mirage";
    }

    public bool IsBoss()
    {
        return TypeInCsv is "Npc_Boss" or "Npc_Boss_TownCrush";
    }

    public bool IsTrain()
    {
        return TypeInCsv == "Train";
    }

    public bool IsRoboWars()
    {
        return TypeInCsv == "RoboWars";
    }

    public bool IsBase()
    {
        return TypeInCsv == "Pvp_Base";
    }

    public bool IsLootBox()
    {
        return TypeInCsv == "LootBox";
    }

    public bool IsTrainingDummy()
    {
        return TypeInCsv == "Minion_Building_charges_ulti";
    }

    public bool IsTownCrushBoss()
    {
        return TypeInCsv == "Npc_Boss_TownCrush";
    }

    public bool IsCarryable()
    {
        return TypeInCsv == "Carryable";
    }

    public bool IsHero()
    {
        return TypeInCsv == "Hero";
    }

    public bool IsPet()
    {
        return Pet != null!;
    }

    public bool IsMinionDuplicate()
    {
        return TypeInCsv == "Minion_Duplicate";
    }

    public bool IsMinionDog()
    {
        return TypeInCsv == "Minion_Dog";
    }

    public bool IsPayload()
    {
        return TypeInCsv == "Payload";
    }

    public bool HasAutoAttack()
    {
        return AutoAttackDamage > 0;
    }

    public bool HasVeryMuchHitPoints()
    {
        return Hitpoints > 5799;
    }

    public override string ToString()
    {
        return $"""
                LogicCharacterData =>
                   Name = {Name},
                   LockedForChronos = {LockedForChronos},
                   Disabled = {Disabled},
                   ItemName = {ItemName},
                   WeaponSkill = {WeaponSkill},
                   UltimateSkill = {UltimateSkill},
                   Pet = {Pet},
                   Speed = {Speed},
                   Hitpoints = {Hitpoints},
                   MeleeAutoAttackSplashDamage = {MeleeAutoAttackSplashDamage},
                   AutoAttackSpeedMs = {AutoAttackSpeedMs},
                   AutoAttackDamage = {AutoAttackDamage},
                   AutoAttackBulletsPerShot = {AutoAttackBulletsPerShot},
                   AutoAttackMode = {AutoAttackMode},
                   AutoAttackProjectileSpread = {AutoAttackProjectileSpread},
                   AutoAttackProjectile = {AutoAttackProjectile},
                   AutoAttackRange = {AutoAttackRange},
                   RegeneratePerSecond = {RegeneratePerSecond},
                   UltiChargeMul = {UltiChargeMul},
                   UltiChargeUltiMul = {UltiChargeUltiMul},
                   Type = {TypeInCsv},
                   DamagerPercentFromAliens = {DamagerPercentFromAliens},
                   DefaultSkin = {DefaultSkin},
                   FileName = {FileName},
                   BlueExportName = {BlueExportName},
                   RedExportName = {RedExportName},
                   ShadowExportName = {ShadowExportName},
                   AreaEffect = {AreaEffect},
                   DeathAreaEffect = {DeathAreaEffect},
                   TakeDamageEffect = {TakeDamageEffect},
                   DeathEffect = {DeathEffect},
                   MoveEffect = {MoveEffect},
                   ReloadEffect = {ReloadEffect},
                   OutOfAmmoEffect = {OutOfAmmoEffect},
                   DryFireEffect = {DryFireEffect},
                   SpawnEffect = {SpawnEffect},
                   MeleeHitEffect = {MeleeHitEffect},
                   AutoAttackStartEffect = {AutoAttackStartEffect},
                   BoneEffect1 = {BoneEffect1},
                   BoneEffect2 = {BoneEffect2},
                   BoneEffect3 = {BoneEffect3},
                   BoneEffect4 = {BoneEffect4},
                   BoneEffectUse = {BoneEffectUse},
                   LoopedEffect = {LoopedEffect},
                   LoopedEffect2 = {LoopedEffect2},
                   KillCelebrationSoundVO = {KillCelebrationSoundVo},
                   InLeadCelebrationSoundVO = {InLeadCelebrationSoundVo},
                   StartSoundVO = {StartSoundVo},
                   UseUltiSoundVO = {UseUltiSoundVo},
                   TakeDamageSoundVO = {TakeDamageSoundVo},
                   DeathSoundVO = {DeathSoundVo},
                   AttackSoundVO = {AttackSoundVo},
                   AttackStartEffectOffset = {AttackStartEffectOffset},
                   TwoWeaponAttackEffectOffset = {TwoWeaponAttackEffectOffset},
                   ShadowScaleX = {ShadowScaleX},
                   ShadowScaleY = {ShadowScaleY},
                   ShadowX = {ShadowX},
                   ShadowY = {ShadowY},
                   ShadowSkew = {ShadowSkew},
                   Scale = {Scale},
                   HeroScreenScale = {HeroScreenScale},
                   FitToBoxScale = {FitToBoxScale},
                   EndScreenScale = {EndScreenScale},
                   GatchaScreenScale = {GatchaScreenScale},
                   HomeScreenScale = {HomeScreenScale},
                   HeroScreenXOffset = {HeroScreenXOffset},
                   HeroScreenZOffset = {HeroScreenZOffset},
                   CollisionRadius = {CollisionRadius},
                   HealthBar = {HealthBar},
                   HealthBarOffsetY = {HealthBarOffsetY},
                   FlyingHeight = {FlyingHeight},
                   ProjectileStartZ = {ProjectileStartZ},
                   StopMovementAfterMS = {StopMovementAfterMs},
                   WaitMS = {WaitMs},
                   TID = {Tid},
                   HeroBundleTID = {HeroBundleTid},
                   ForceAttackAnimationToEnd = {ForceAttackAnimationToEnd},
                   IconSWF = {IconSwf},
                   IconExportName = {IconExportName},
                   RecoilAmount = {RecoilAmount},
                   Homeworld = {Homeworld},
                   FootstepClip = {FootstepClip},
                   DifferentFootstepOffset = {DifferentFootstepOffset},
                   FootstepIntervalMS = {FootstepIntervalMs},
                   AttackingWeaponScale = {AttackingWeaponScale},
                   UseThrowingLeftWeaponBoneScaling = {UseThrowingLeftWeaponBoneScaling},
                   UseThrowingRightWeaponBoneScaling = {UseThrowingRightWeaponBoneScaling},
                   CommonSetUpgradeBonus = {CommonSetUpgradeBonus},
                   RareSetUpgradeBonus = {RareSetUpgradeBonus},
                   SuperRareSetUpgradeBonus = {SuperRareSetUpgradeBonus},
                   CanWalkOverWater = {CanWalkOverWater},
                   UseColorMod = {UseColorMod},
                   RedAdd = {RedAdd},
                   GreenAdd = {GreenAdd},
                   BlueAdd = {BlueAdd},
                   RedMul = {RedMul},
                   GreenMul = {GreenMul},
                   BlueMul = {BlueMul},
                   ChargeUltiAutomatically = {ChargeUltiAutomatically},
                   VideoLink = {VideoLink},
                   ShouldEncodePetStatus = {ShouldEncodePetStatus},
                   SecondaryPet = {SecondaryPet},
                   ExtraMinions = {ExtraMinions},
                   PetAutoSpawnDelay = {PetAutoSpawnDelay}
                """;
    }
}