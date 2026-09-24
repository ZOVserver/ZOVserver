using MessagePack;
using Orleans;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;

namespace ZOVserver.Shared.Contracts.Models;

[MessagePackObject]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Models.DetailedAllianceMemberHomeModel")]
public class DetailedAllianceMemberHomeModel
{
    [Key(0)] [Id(0)] public PlayerDisplayData DisplayData { get; set; } = null!;
    [Key(1)] [Id(1)] public int Status { get; set; }
    [Key(2)] [Id(2)] public DateTime LastOnlineTime { get; set; }
    [Key(3)] [Id(3)] public int Trophies { get; set; }
    [Key(4)] [Id(4)] public bool InvitesBlocked { get; set; }
}