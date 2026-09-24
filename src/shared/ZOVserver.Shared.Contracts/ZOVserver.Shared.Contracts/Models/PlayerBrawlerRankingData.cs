using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;

namespace ZOVserver.Shared.Contracts.Models;

[MessagePackObject]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Models.PlayerBrawlerRankingData")]
public class PlayerBrawlerRankingData
{
    [Key(0)] [Id(0)] public long AccountId { get; set; }
    [Key(1)] [Id(1)] public int BrawlerGlobalId { get; set; }
    [Key(2)] [Id(2)] public int BrawlerTrophies { get; set; }
    [Key(3)] [Id(3)] public PlayerDisplayData? DisplayData { get; set; }
    [Key(4)] [Id(4)] public string? AllianceName { get; set; }
}