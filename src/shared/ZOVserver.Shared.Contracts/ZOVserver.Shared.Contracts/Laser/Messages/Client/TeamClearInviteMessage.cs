using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamClearInviteMessage : PiranhaMessage
{
    [Field(0)] public long InviteId { get; set; }

    public override int GetMessageType()
    {
        return 14367;
    }

    public override int GetServiceNodeType()
    {
        return 23;
    }
}