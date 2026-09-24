using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Entries;

[MessagePackObject]
[LaserSerializable]
public partial class HeroDataEntry : LaserContract
{
    [Key(0)] [Field(0)] public int BrawlerGlobalId { get; set; }

    [Key(1)] [Field(1)] public int SkinGlobalId { get; set; }

    [Key(2)] [Field(2, IsVarInt = true)] public int Team { get; set; }

    [Key(3)] [Field(3)] public bool IsPlayer { get; set; }

    [Key(4)] [Field(4)] public string PlayerName { get; set; } = string.Empty;
}