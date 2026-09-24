using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Team;

[LaserSerializable]
[GenerateSerializer]
[Alias("Team.TeamMemberEntry")]
public partial class TeamMemberEntry : LaserContract
{
    [Field(0)] [Id(0)] public bool IsOwner { get; set; }
    [Field(1)] [Id(1)] public long AccountId { get; set; }

    [Field(2, AsDataRef = true)] [Id(2)] public int CharacterGlobalId { get; set; }
    [Field(3, AsDataRef = true)] [Id(3)] public int SkinGlobalId { get; set; }

    [Field(4, IsVarInt = true)] [Id(4)] public int HeroTrophies { get; set; }
    [Field(5, IsVarInt = true)] [Id(5)] public int HeroMaxTrophies { get; set; }
    [Field(6, IsVarInt = true)] [Id(6)] public int HeroPowerLevel { get; set; }

    [Field(7, IsVarInt = true)] [Id(7)] public int State { get; set; }
    [Field(8)] [Id(8)] public bool IsReady { get; set; }

    [Field(9, IsVarInt = true)] [Id(9)] public int TeamIndex { get; set; }
    [Field(10, IsVarInt = true)] [Id(10)] public int DifficultyLevel { get; set; }
    [Field(11, IsVarInt = true)] [Id(11)] public int Unk3 { get; set; }

    [Field(12)] [Id(12)] public PlayerDisplayData DisplayData { get; set; } = null!;

    [Field(13, AsDataRef = true)] [Id(13)] public int StarPowerGlobalId { get; set; }
}