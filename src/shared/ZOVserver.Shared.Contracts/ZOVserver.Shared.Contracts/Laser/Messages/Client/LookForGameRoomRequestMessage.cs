using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class LookForGameRoomRequestMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int EventId { get; set; }
    [Field(1, IsVarInt = true)] public int EventSlot { get; set; }

    public override int GetMessageType()
    {
        return 14199;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}