using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamSetEventMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int LocationId { get; set; }
    [Field(1, IsVarInt = true)] public int EventSlot { get; set; }

    public override int GetMessageType()
    {
        return 14362;
    }

    public override int GetServiceNodeType()
    {
        return 23;
    }
}