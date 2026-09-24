using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Entries;

[MessagePackObject]
[LaserSerializable]
public partial class ChronosTextEntry : LaserContract
{
    [Key(0)] [Field(0)] public int Type { get; set; }

    [Key(1)] [Field(1)] public string Text { get; set; } = string.Empty;
}