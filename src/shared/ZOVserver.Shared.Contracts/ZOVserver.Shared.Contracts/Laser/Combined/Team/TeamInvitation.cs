using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Team;

[LaserSerializable]
public partial class TeamInvitation : LaserContract
{
    [Field(1)] public long TeamId { get; set; }
    [Field(2)] public FriendEntry FriendEntry { get; set; } = null!;
}