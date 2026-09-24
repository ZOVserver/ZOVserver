using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class ServerHelloMessage : PiranhaMessage
{
    [Field(0)] public byte[] ServerHelloToken { get; set; } = [];

    public byte ChsByte { get; init; }

    public override int GetMessageType()
    {
        return 20100;
    }

    public override int GetServiceNodeType()
    {
        return 1;
    }
}