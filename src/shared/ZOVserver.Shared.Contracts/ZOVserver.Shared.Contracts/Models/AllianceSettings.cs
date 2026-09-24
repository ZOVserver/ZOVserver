using MessagePack;
using Orleans;

namespace ZOVserver.Shared.Contracts.Models;

[MessagePackObject]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Models.AllianceSettings")]
public class AllianceSettings
{
    [Key(0)] [Id(0)] public string Description { get; set; } = string.Empty;

    [Key(1)] [Id(1)] public int BadgeGlobalId { get; set; }

    [Key(2)] [Id(2)] public int RegionGlobalId { get; set; }

    [Key(3)] [Id(3)] public int AllianceType { get; set; }

    [Key(4)] [Id(4)] public int RequiredTrophies { get; set; }
}