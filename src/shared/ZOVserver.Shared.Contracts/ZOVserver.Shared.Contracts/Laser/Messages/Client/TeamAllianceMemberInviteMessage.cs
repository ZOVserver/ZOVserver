using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamAllianceMemberInviteMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public long PlayerId { get; set; }
    [Field(1, IsVarInt = true)] public int TeamIndex { get; set; }

    public override int GetMessageType()
    {
        return 14370;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}