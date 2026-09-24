using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;
using ZOVserver.Shared.TitanRemnants.Mathem;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Entries;

[MessagePackObject]
[LaserSerializable]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Laser.Combined.Entries.HeroEntry")]
public partial class HeroEntry : LaserContract
{
    public HeroEntry(int characterDataId)
    {
        CharacterGlobalId = characterDataId;
        CardUnlockGlobalId = LogicDataTables.GetDataByName(23,
            LogicDataTables.GetDataById(characterDataId)!.Name + "_unlock")!.GlobalId;
    }

    public HeroEntry()
    {
    }

    [Key(0)] [Field(0)] [Id(0)] public int CharacterGlobalId { get; set; }

    [Key(8)] [Field(1)] [Id(1)] public int UnkDataRef { get; set; }

    [Key(1)] [Id(2)] public int CardUnlockGlobalId { get; set; }

    [Key(2)] [Id(3)] public int CharacterState { get; set; } = 1;

    [Key(3)]
    [Field(2, IsVarInt = true)]
    [Id(4)]
    public int Trophies { get; set; }

    [Key(4)]
    [Field(3, IsVarInt = true)]
    [Id(5)]
    public int MaxTrophies { get; set; }

    [Key(5)]
    [Field(4, IsVarInt = true, AddNumber = 1)]
    [Id(6)]
    public int PowerLevel { get; set; } = 1;

    [Key(6)] [Id(7)] public int PowerPoints { get; set; }

    [Key(7)] [Id(8)] public Dictionary<int, bool> StarPowersContainer { get; set; } = [];

    public void AddTrophies(int value)
    {
        Trophies += value;
        MaxTrophies = LogicMath.Max(MaxTrophies, Trophies);
    }

    public void AddCard(int globalId)
    {
        if (globalId <= 1000000) globalId = GlobalId.CreateGlobalId(23, globalId);

        if (LogicDataTables.GetDataById<LogicCardData>(globalId)!.Name.Contains("_unique"))
            StarPowersContainer.TryAdd(globalId, StarPowersContainer.Count == 0);
    }

    public bool GetCard(int globalId, out bool selected)
    {
        if (globalId <= 1000000) globalId = GlobalId.CreateGlobalId(23, globalId);

        if (LogicDataTables.GetDataById<LogicCardData>(globalId)!.Name.Contains("_unique"))
            return StarPowersContainer.TryGetValue(globalId, out selected);

        selected = false;
        return false;
    }
}