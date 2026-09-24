using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class JoinAllianceUsingInvitationMessage : PiranhaMessage
{
    [Field(0)] public long AvatarStreamEntryId { get; set; }

    public override int GetMessageType()
    {
        return 14323;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}