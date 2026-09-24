using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;

[MessagePackObject]
[LaserSerializable]
public partial class GatchaDrop : LaserContract
{
    [Key(0)] [Field(0, IsVarInt = true)] public int Count { get; set; }

    [Key(1)] [Field(2, IsVarInt = true)] public int Type { get; set; }

    [Key(2)] [Field(1)] public int HeroGlobalId { get; set; }

    [Key(3)] [Field(3)] public int SkinGlobalId { get; set; }

    [Key(4)] [Field(4)] public int CardGlobalId { get; set; }

    [Key(5)] [Field(5, IsVarInt = true)] public int Unk20 { get; set; }
}