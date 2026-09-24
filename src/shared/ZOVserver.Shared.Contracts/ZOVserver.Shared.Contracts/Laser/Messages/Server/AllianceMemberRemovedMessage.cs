using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AllianceMemberRemovedMessage : PiranhaMessage
{
    [Field(0)] public long AllianceId { get; set; }
    [Field(1)] public long AccountId { get; set; }

    public override int GetMessageType()
    {
        return 24309;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}