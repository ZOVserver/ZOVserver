using Orleans;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Team;

[GenerateSerializer]
[Alias("Team.TeamMemberData")]
public class TeamMemberData
{
    [Id(1)] public PlayerDisplayData DisplayData { get; set; } = null!;

    [Id(2)] public int CharacterGlobalId { get; set; }
    [Id(3)] public int SkinGlobalId { get; set; }

    [Id(4)] public int HeroTrophies { get; set; }
    [Id(5)] public int HeroMaxTrophies { get; set; }
    [Id(6)] public int HeroPowerLevel { get; set; }

    [Id(7)] public int StarPowerGlobalId { get; set; }

    [Id(8)] public int DifficultyLevel { get; set; }
}