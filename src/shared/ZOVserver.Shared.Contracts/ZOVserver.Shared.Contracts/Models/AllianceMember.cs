using MessagePack;
using Orleans;

namespace ZOVserver.Shared.Contracts.Models;

[MessagePackObject]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Models.AllianceMember")]
public class AllianceMember
{
    [Key(0)] [Id(0)] public long AccountId { get; set; }

    [Key(1)] [Id(1)] public AllianceRole Role { get; set; }

    [Key(2)] [Id(2)] public DateTime JoinTime { get; set; }
}