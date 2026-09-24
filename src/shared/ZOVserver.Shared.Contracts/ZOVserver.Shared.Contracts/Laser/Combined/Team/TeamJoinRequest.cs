using Orleans;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Team;

[LaserSerializable]
[GenerateSerializer]
[Alias("Team.TeamJoinRequest")]
public partial class TeamJoinRequest : LaserContract
{
    [Field(0)] [Id(0)] public long JoinerId { get; set; }
    [Field(1)] [Id(1)] public long TargetId { get; set; }
    [Field(2)] [Id(2)] public FriendEntry FriendEntry { get; set; } = null!;
}