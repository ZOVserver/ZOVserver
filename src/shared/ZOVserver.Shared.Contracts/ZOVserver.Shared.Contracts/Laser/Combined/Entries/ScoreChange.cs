using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Entries;

[MessagePackObject]
[LaserSerializable]
public partial class ScoreChange : LaserContract
{
    [Key(0)] [Field(1)] public int BrawlerGlobalId { get; set; }

    [Key(1)] [Field(0, IsVarInt = true)] public int BrawlerTrophies { get; set; }
}