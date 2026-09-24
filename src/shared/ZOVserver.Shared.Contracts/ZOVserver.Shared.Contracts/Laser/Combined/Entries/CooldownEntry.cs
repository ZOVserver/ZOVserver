using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Entries;

[MessagePackObject]
[LaserSerializable]
public partial class CooldownEntry : LaserContract
{
    [Key(0)] [Field(0)] public int Unk0VInt { get; set; }

    [Key(1)] [Field(1)] public int Unk4DataRef { get; set; }

    [Key(2)] [Field(2)] public int Unk8VInt { get; set; }
}