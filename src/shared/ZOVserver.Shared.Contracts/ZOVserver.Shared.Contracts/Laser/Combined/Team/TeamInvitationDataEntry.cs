using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Team;

[LaserSerializable]
public partial class TeamInvitationDataEntry : LaserContract
{
    [Field(0)] public long TeamId { get; set; }
    [Field(1)] public string PlayerInviterName { get; set; } = string.Empty;
}