using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class TeamInvitationMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int Type { get; set; }
    [Field(1)] public TeamInvitation TeamInvitation { get; set; } = null!;

    public override int GetMessageType()
    {
        return 24589;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}