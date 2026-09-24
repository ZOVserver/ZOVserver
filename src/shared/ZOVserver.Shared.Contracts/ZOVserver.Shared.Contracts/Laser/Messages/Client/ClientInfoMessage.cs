using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class ClientInfoMessage : PiranhaMessage
{
    [Field(0)] public string Interface { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 10177;
    }

    public override int GetServiceNodeType()
    {
        return 27;
    }
}