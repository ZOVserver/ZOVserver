using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Entries;

[MessagePackObject]
[LaserSerializable]
public partial class BattleLogPlayerEntry : LaserContract
{
    [Key(0)] [Field(0, IsVarInt = true)] public int Unk1 { get; set; } = 1; // 1 = visible
    [Key(1)] [Field(1)] public long PlayerId { get; set; }
    [Key(2)] [Field(2, IsVarInt = true)] public int Unk3 { get; set; }
    [Key(3)] [Field(3)] public bool StarPlayer { get; set; }
    [Key(4)] [Field(4, AsDataRef = true)] public int CharacterId { get; set; }
    [Key(5)] [Field(5, IsVarInt = true)] public int HeroTrophies { get; set; }
    [Key(6)] [Field(6, IsVarInt = true)] public int HeroLevel { get; set; }
    [Key(7)] [Field(7, IsVarInt = true)] public int Unk8 { get; set; }
    [Key(8)] [Field(8)] public PlayerDisplayData? PlayerDisplayData { get; set; }
}