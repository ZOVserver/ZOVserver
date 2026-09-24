using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Entries;

[MessagePackObject]
[LaserSerializable]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Laser.Combined.Entries.ProfileStatEntry")]
public partial class ProfileStatEntry : LaserContract
{
    [Key(0)]
    [Field(0, IsVarInt = true)]
    [Id(0)]
    public int Id { get; set; }

    [Key(1)]
    [Field(1, IsVarInt = true)]
    [Id(1)]
    public int Value { get; set; }
}