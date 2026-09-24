using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class SpectateFailedMessage : PiranhaMessage
{
    [Field(0)] public int Unk1 { get; set; }

    public override int GetMessageType()
    {
        return 24105;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}