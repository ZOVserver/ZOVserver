using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class ReportAllianceStreamMessage : PiranhaMessage
{
    [Field(0)] public long Id { get; set; }
    [Field(1)] public long ReportedAvatarId { get; set; }

    public override int GetMessageType()
    {
        return 10119;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}