using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Entries;

[MessagePackObject]
[LaserSerializable]
public partial class ChronosFileEntry : LaserContract
{
    [Key(0)] [Field(0)] public string ImageName { get; set; } = string.Empty;

    [Key(1)] [Field(1)] public string ImageSha1 { get; set; } = string.Empty;
}