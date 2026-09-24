using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Player;

[MessagePackObject]
[LaserSerializable]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Laser.Combined.Player.LogicHeroUpgrades")]
public partial class LogicHeroUpgrades : LaserContract
{
    [Field(0, IsVarInt = true)]
    [Key(0)]
    [Id(0)]
    public int UnkVInt { get; set; }

    [Field(1, AsDataRef = true)]
    [Key(1)]
    [Id(1)]
    public int StarPowerGlobalId { get; set; }
}