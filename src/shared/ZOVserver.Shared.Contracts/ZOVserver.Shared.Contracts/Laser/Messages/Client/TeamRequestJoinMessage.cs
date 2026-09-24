using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamRequestJoinMessage : PiranhaMessage
{
    [Field(0)] public long PlayerId { get; set; }
    [Field(1)] public long TeamId { get; set; }

    public override int GetMessageType()
    {
        return 14881;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}