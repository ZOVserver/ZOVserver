using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;

[MessagePackObject]
[LaserSerializable]
public partial class AdStatus : LaserContract
{
    [Key(0)] [Field(0, IsVarInt = true)] public int Unk0 { get; set; }

    [Key(1)] [Field(1, IsVarInt = true)] public int Unk4 { get; set; }

    [Key(2)] [Field(2, IsVarInt = true)] public int Unk8 { get; set; }
}