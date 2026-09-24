using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class TeamStreamEntryRemovedMessage : PiranhaMessage
{
    [Field(0)] public long TeamId { get; set; }
    [Field(1)] public long StreamId { get; set; }

    public override int GetMessageType()
    {
        return 24319;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}