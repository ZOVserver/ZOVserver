using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AllianceStreamEntryRemovedMessage : PiranhaMessage
{
    [Field(0)] public long StreamId { get; set; }

    public override int GetMessageType()
    {
        return 24318;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}