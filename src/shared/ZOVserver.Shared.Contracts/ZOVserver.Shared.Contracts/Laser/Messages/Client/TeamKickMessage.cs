using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamKickMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public long MemberId { get; set; }

    public override int GetMessageType()
    {
        return 14352;
    }

    public override int GetServiceNodeType()
    {
        return 23;
    }
}