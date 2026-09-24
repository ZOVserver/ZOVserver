using MessagePack;
using Orleans;

namespace ZOVserver.Shared.Contracts.Models;

[MessagePackObject]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Models.AllianceRankingData")]
public class AllianceRankingData
{
    [Key(0)] [Id(0)] public long AllianceId { get; set; }
    [Key(1)] [Id(1)] public int Trophies { get; set; }
    [Key(2)] [Id(2)] public string? AllianceName { get; set; }
    [Key(3)] [Id(3)] public int BadgeGlobalId { get; set; }
    [Key(4)] [Id(4)] public int MembersCount { get; set; }
}