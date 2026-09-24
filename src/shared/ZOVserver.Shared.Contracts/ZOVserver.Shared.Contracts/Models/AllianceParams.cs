using MessagePack;
using Orleans;

namespace ZOVserver.Shared.Contracts.Models;

[MessagePackObject]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Models.AllianceParams")]
public class AllianceParams
{
    [Key(0)] [Id(0)] public long AllianceId { get; set; }

    [Key(1)] [Id(1)] public string Name { get; set; } = string.Empty;

    [Key(2)] [Id(2)] public string Description { get; set; } = string.Empty;

    [Key(3)] [Id(3)] public long OwnerAccountId { get; set; }

    [Key(4)] [Id(4)] public int AllianceType { get; set; }

    [Key(5)] [Id(5)] public int RegionGlobalId { get; set; }

    [Key(6)] [Id(6)] public int LanguageGlobalId { get; set; }

    [Key(7)] [Id(7)] public int BadgeGlobalId { get; set; }

    [Key(8)] [Id(8)] public int RequiredTrophies { get; set; }
}