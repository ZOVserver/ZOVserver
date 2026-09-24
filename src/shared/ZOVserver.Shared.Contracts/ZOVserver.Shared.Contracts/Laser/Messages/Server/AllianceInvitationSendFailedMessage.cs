using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AllianceInvitationSendFailedMessage : PiranhaMessage
{
    [Field(0)] public int Reason { get; set; }

    public override int GetMessageType()
    {
        return 24321;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}