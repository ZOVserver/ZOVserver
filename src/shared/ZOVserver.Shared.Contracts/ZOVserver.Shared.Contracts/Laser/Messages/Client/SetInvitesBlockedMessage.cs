using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class SetInvitesBlockedMessage : PiranhaMessage
{
    [Field(0)] public bool State { get; set; }

    public override int GetMessageType()
    {
        return 14777;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}