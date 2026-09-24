using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;

[LaserSerializable]
public partial class Sub1Aa808 : LaserContract
{
    [Field(0, IsVarInt = true)] public int Unk0 { get; set; }

    [Field(1, IsVarInt = true)] public int Unk4 { get; set; }

    [Field(2, IsVarInt = true)] public int Unk8 { get; set; }

    [Field(3, IsVarInt = true)] public int Unk12 { get; set; }

    [Field(4)] public bool Unk16 { get; set; }
}