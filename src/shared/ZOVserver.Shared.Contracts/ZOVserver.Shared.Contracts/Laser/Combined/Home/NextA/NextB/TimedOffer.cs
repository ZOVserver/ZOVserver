using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;

[MessagePackObject]
[LaserSerializable]
public partial class TimedOffer : LaserContract
{
    [Key(0)] [Field(0)] public int Unk0DataRef { get; set; }

    [Key(1)] [Field(1)] public int Unk4 { get; set; }

    [Key(2)] [Field(2)] public int Unk8 { get; set; }
}