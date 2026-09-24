using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AddFriendFailedMessage : PiranhaMessage
{
    [Field(0)] public int ErrorCode { get; set; }

    public override int GetMessageType()
    {
        return 20112;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}