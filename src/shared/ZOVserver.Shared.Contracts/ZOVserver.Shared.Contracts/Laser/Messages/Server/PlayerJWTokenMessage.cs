using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class PlayerJwTokenMessage : PiranhaMessage
{
    [Field(0)] public string Jwt { get; set; } = "{}";

    public override int GetMessageType()
    {
        return 23774;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}