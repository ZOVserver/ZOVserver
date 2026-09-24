using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class JoinAllianceMessage : PiranhaMessage
{
    [Field(0)] public long AllianceId { get; set; }

    public override int GetMessageType()
    {
        return 14305;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}