using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamInviteResponseMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int UnkVInt1 { get; set; }
    [Field(1, IsVarInt = true)] public int UnkVInt2 { get; set; }

    public override int GetMessageType()
    {
        return 14368;
    }

    public override int GetServiceNodeType()
    {
        return 23;
    }
}