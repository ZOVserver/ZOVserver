using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Entries;

[MessagePackObject]
[LaserSerializable]
public partial class ScoreEntry : LaserContract
{
    [Key(0)]
    [Field(0, Type = FieldType.VInt32)]
    public int BrawlerGlobalId { get; set; }

    [Key(1)] [Field(1, IsVarInt = true)] public int BrawlerTrophies { get; set; }

    [Key(2)] [Field(2, IsVarInt = true)] public int BrawlerTrophyLoss { get; set; }

    [Key(3)] [Field(3, IsVarInt = true)] public int StarPointsGained { get; set; }
}