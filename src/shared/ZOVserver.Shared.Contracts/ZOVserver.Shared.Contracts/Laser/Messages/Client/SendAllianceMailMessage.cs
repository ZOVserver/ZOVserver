using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class SendAllianceMailMessage : PiranhaMessage
{
    [Field(0)] public int UnkInt { get; set; }
    [Field(1)] public string Message { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 14330;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}