using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;

namespace ZOVserver.Shared.Contracts.Models;

[MessagePackObject]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Models.DetailedAllianceModel")]
public class DetailedAllianceModel
{
    [Key(0)] [Id(0)] public AllianceParams AllianceParams { get; set; } = null!;
    [Key(1)] [Id(1)] public DetailedAllianceMemberModel[]? AllianceMembers { get; set; }
    [Key(2)] [Id(2)] public AllianceTeamEntry[] TeamEntries { get; set; } = [];
}