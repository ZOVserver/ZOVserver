using MessagePack;
using Orleans;

namespace ZOVserver.Shared.Contracts.Models;

[MessagePackObject]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Models.DetailedAllianceMemberModel")]
public class DetailedAllianceMemberModel
{
    [Key(0)] [Id(0)] public AllianceMember AllianceMember { get; set; } = null!;
    [Key(1)] [Id(1)] public DetailedAllianceMemberHomeModel HomeModel { get; set; } = null!;
}