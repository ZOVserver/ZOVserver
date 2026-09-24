using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Stream.Inheritors.Target;

[MessagePackObject]
[LaserSerializable]
[GenerateSerializer]
[Alias("Target.EventStreamTargetEntry")]
public partial class EventStreamTargetEntry : LaserContract
{
    [Key(200)]
    [Field(0, IsVarInt = true)]
    [Id(0)]
    public long AccountId { get; set; }

    [Key(201)] [Field(1)] [Id(1)] public string Name { get; set; } = string.Empty;
}