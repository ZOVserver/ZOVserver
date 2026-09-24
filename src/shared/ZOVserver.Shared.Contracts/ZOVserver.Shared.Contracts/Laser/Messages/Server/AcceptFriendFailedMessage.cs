using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AcceptFriendFailedMessage : PiranhaMessage
{
    [Field(0)] public long FriendId { get; set; }
    [Field(1)] public int ErrorCode { get; set; }

    public override int GetMessageType()
    {
        return 20501;
    }

    public override int GetServiceNodeType()
    {
        return 26;
    }
}