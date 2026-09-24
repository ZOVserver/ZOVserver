using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class TeamInviteStatusMessage : PiranhaMessage
{
    [Field(0)] public int Reason { get; set; }
    [Field(1)] public TeamInvitationDataEntry TeamInvitationDataEntry { get; set; } = null!;

    public override int GetMessageType()
    {
        return 24582;
    }

    public override int GetServiceNodeType()
    {
        return 23;
    }
}