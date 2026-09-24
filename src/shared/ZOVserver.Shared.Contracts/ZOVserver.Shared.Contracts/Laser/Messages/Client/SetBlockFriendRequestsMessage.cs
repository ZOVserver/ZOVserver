using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class SetBlockFriendRequestsMessage : PiranhaMessage
{
    [Field(0)] public bool State { get; set; }

    public override int GetMessageType()
    {
        return 10576;
    }

    public override int GetServiceNodeType()
    {
        return 26;
    }
}