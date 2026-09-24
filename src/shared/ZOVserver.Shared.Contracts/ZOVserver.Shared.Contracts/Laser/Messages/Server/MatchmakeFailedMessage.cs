using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class MatchmakeFailedMessage : PiranhaMessage
{
    [Field(0)] public int ErrorCode { get; set; }

    public override int GetMessageType()
    {
        return 24108;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}