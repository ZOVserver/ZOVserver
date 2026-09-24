using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Entries;

[MessagePackObject]
[LaserSerializable]
public partial class ReleaseEntry : LaserContract
{
    [Key(0)] [Field(0)] public int BrawlerGlobalId { get; set; }

    [Key(1)]
    [Field(1, CalculateSecondsLeft = true)]
    public DateTime ReleaseEndTime { get; set; }

    [Key(2)] [Field(2)] public int Unk1 { get; set; } = -1;
}