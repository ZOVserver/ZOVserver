using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Player;

[LaserSerializable]
public partial class PlayerProfile : LaserContract
{
    [Field(0, IsVarInt = true)] public long AccountId { get; set; }
    [Field(1, AsDataRef = true)] public int UnkDataRef { get; set; }
    [Field(2)] public HeroEntry[] HeroEntries { get; set; } = [];
    [Field(3)] public ProfileStatEntry[] StatEntries { get; set; } = [];
    [Field(4)] public PlayerDisplayData? DisplayData { get; set; }
}