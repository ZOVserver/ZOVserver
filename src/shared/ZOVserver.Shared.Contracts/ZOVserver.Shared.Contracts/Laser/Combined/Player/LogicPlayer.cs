using System.Numerics;
using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Player;

[MessagePackObject]
[LaserSerializable]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Laser.Combined.Player.LogicPlayer")]
public partial class LogicPlayer : LaserContract
{
    [Field(0)] [Key(0)] [Id(0)] public long AccountId { get; set; }

    [Field(1, IsVarInt = true)]
    [Key(1)]
    [Id(1)]
    public int PlayerIndex { get; set; }

    [Field(2, IsVarInt = true)]
    [Key(2)]
    [Id(2)]
    public int TeamIndex { get; set; }

    [Field(3, IsVarInt = true)]
    [Key(3)]
    [Id(3)]
    public int Unk1 { get; set; }

    [Field(4)] [Key(4)] [Id(4)] public int Unk2 { get; set; }

    [Field(5, AsDataRef = true)]
    [Key(5)]
    [Id(5)]
    public int CharacterGlobalId { get; set; }

    [Field(6, AsDataRef = true)]
    [Key(6)]
    [Id(6)]
    public int SkinGlobalId { get; set; }

    [Field(7, PresenceBool = true)]
    [Key(7)]
    [Id(7)]
    public LogicHeroUpgrades? HeroUpgrades { get; set; }

    [Field(8)] [Key(8)] [Id(8)] public PlayerDisplayData? DisplayData { get; set; }

    [field: NonSerialized] [IgnoreMember] public uint MyObjectRunningId { get; set; }

    [field: NonSerialized] [IgnoreMember] public int MyObjectIndex { get; set; }

    [field: NonSerialized] [IgnoreMember] public int LastInput { get; set; }

    [field: NonSerialized] [IgnoreMember] public bool IsAlive { get; set; }

    [field: NonSerialized] [IgnoreMember] public List<PlayerKillEntry> KillList { get; set; } = [];

    [field: NonSerialized] [IgnoreMember] public int Kills { get; private set; }
    [field: NonSerialized] [IgnoreMember] public int Damage { get; private set; }
    [field: NonSerialized] [IgnoreMember] public int Heals { get; private set; }
    [field: NonSerialized] [IgnoreMember] private int Score { get; set; }

    [field: NonSerialized] [IgnoreMember] public Vector3 SpawnPoint { get; set; }

    [field: NonSerialized]
    [IgnoreMember]
    public int UltiCharge
    {
        get;
        private set => field = Math.Clamp(value, 0, 4000);
    }

    public void ChargeUlti(int chargeAmount, bool isUltimateAction, bool ignoreModifiers,
        LogicCharacterData characterData)
    {
        var multiplier = ignoreModifiers ? 100 :
            isUltimateAction ? characterData.UltiChargeUltiMul : characterData.UltiChargeMul;

        UltiCharge += chargeAmount * multiplier / 100;
    }

    public bool HasUlti()
    {
        return UltiCharge >= 4000;
    }

    public int GetCardValueForPassive(string a2, int a3)
    {
        if (HeroUpgrades == null)
            return -1;

        var starPowerData = LogicDataTables.GetDataById<LogicCardData>(HeroUpgrades.StarPowerGlobalId);

        if (starPowerData?.TypeInCsv != a2)
            return -1;

        if (a3 == 2)
            return starPowerData.Value3;

        return a3 > 0 ? starPowerData.Value : starPowerData.Value2;
    }

    public int GetScore()
    {
        return Score;
    }

    public void AddScore(int score)
    {
        Score += score;
    }

    public void ResetScore()
    {
        Score = 0;
    }

    public void Healed(int heals)
    {
        Heals += heals;
    }

    public void DamageDealed(int damage)
    {
        Damage += damage;
    }

    public void KilledPlayer(int index, int bountyStars)
    {
        KillList.Add(new PlayerKillEntry
        {
            PlayerIndex = index,
            BountyStarsEarned = bountyStars
        });

        Kills++;
    }
}