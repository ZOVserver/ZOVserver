using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AllianceTeamRemovedMessage : PiranhaMessage
{
    [Field(0)] public long TeamId { get; set; }

    public override int GetMessageType()
    {
        return 24365;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}