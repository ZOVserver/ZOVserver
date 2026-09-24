using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class DisconnectedMessage : PiranhaMessage
{
    [Field(0)] public int Reason { get; set; }

    public override int GetMessageType()
    {
        return 25892;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}