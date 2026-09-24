using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Player;

[MessagePackObject]
[LaserSerializable]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Laser.Combined.Player.PlayerDisplayData")]
public partial class PlayerDisplayData : LaserContract
{
    [Key(0)] [Field(0)] [Id(0)] public string AvatarName { get; set; } = string.Empty;

    [Key(1)]
    [Field(1, IsVarInt = true)]
    [Id(1)]
    public int Experience { get; set; }

    [Key(2)]
    [Field(2, IsVarInt = true)]
    [Id(2)]
    public int Thumbnail { get; set; }

    [Key(3)]
    [Field(3, IsVarInt = true)]
    [Id(3)]
    public int NameColor { get; set; }
}