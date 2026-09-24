using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;

[MessagePackObject]
[LaserSerializable]
public partial class IntValueEntry : LaserContract
{
    [Key(0)] [Field(0)] public int Key { get; set; }

    [Key(1)] [Field(1)] public int Value { get; set; }
}