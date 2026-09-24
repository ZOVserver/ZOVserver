using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class ServerErrorMessage : PiranhaMessage
{
    [Field(0)] public int Error { get; set; }

    public override int GetMessageType()
    {
        return 24115;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}