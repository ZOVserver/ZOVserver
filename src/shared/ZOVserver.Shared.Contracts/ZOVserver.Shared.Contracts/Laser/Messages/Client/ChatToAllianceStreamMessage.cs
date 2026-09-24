using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class ChatToAllianceStreamMessage : PiranhaMessage
{
    [Field(0)] public string Message { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 14315;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}