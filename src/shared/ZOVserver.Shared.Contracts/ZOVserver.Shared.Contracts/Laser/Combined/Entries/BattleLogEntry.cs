using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Entries;

[MessagePackObject]
[LaserSerializable]
public partial class BattleLogEntry : LaserContract
{
    [Key(0)] [Field(0, IsVarInt = true)] public int Unk1 { get; set; }

    [Key(1)]
    [Field(1, IsVarInt = true, CalculateSecondsPassed = true)]
    public DateTime BattleEndTime { get; set; }

    [Key(2)] [Field(2, IsVarInt = true)] public int LogType { get; set; } // 1 or 3(survive)
    [Key(3)] [Field(3, IsVarInt = true)] public int TrophiesGained { get; set; }
    [Key(4)] [Field(4, IsVarInt = true)] public int BattleSeconds { get; set; }

    [Key(5)] [Field(5)] public bool IsFriendly { get; set; }

    [Key(6)] [Field(6, AsDataRef = true)] public int LocationId { get; set; }

    [Key(7)] [Field(7, IsVarInt = true)] public int BattleResult { get; set; } // 0 = victory 1 = defeat 2 = draw
    [Key(8)] [Field(8, IsVarInt = true)] public int Unk9 { get; set; }

    [Key(9)] [Field(9)] public long Unk10 { get; set; }
    [Key(10)] [Field(10)] public long Unk11 { get; set; }

    [Key(11)] [Field(11, IsVarInt = true)] public int Unk12 { get; set; }
    [Key(12)] [Field(12)] public bool Unk13 { get; set; }

    [Key(13)] [Field(13)] public BattleLogPlayerEntry[] BattleLogPlayerEntries { get; set; } = [];

    [Key(14)] [Field(14, IsVarInt = true)] public int Unk15 { get; set; }
    [Key(15)] [Field(15)] public bool IsPowerLeague { get; set; }
    [Key(16)] [Field(16)] public bool IsChampionship { get; set; }
}