using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class ShutdownStartedMessage : PiranhaMessage
{
    [Field(0)] public int Unk1 { get; set; }

    public override int GetMessageType()
    {
        return 20161;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}