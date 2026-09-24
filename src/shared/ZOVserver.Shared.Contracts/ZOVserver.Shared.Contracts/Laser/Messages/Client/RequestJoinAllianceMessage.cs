using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class RequestJoinAllianceMessage : PiranhaMessage
{
    [Field(0)] public long AllianceId { get; set; }
    [Field(1)] public string RequestText { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 14317;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}