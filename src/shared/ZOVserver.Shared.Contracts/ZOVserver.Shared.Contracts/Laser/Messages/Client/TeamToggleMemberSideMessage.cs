using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamToggleMemberSideMessage : PiranhaMessage
{
    [Field(0)] public long[] MembersId { get; set; } = [];
    [Field(1, IsVarInt = true)] public int Side { get; set; }

    public override int GetMessageType()
    {
        return 14357;
    }

    public override int GetServiceNodeType()
    {
        return 23;
    }
}