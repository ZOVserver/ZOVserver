using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class RespondToAllianceJoinRequestMessage : PiranhaMessage
{
    [Field(0)] public long StreamId { get; set; }
    [Field(1)] public bool Accepted { get; set; }

    public override int GetMessageType()
    {
        return 14321;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}