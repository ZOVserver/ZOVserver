using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Entries;

[MessagePackObject]
[LaserSerializable]
public partial class SupportedCreatorEntry : LaserContract
{
    [Key(0)] [Field(0)] public string Code { get; set; } = string.Empty;
}