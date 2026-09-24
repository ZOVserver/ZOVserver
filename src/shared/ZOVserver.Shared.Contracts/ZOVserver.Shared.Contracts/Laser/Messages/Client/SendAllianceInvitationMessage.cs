using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class SendAllianceInvitationMessage : PiranhaMessage
{
    [Field(0)] public long AvatarId { get; set; }

    public override int GetMessageType()
    {
        return 14322;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}