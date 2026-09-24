using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class ChangeAllianceMemberRoleMessage : PiranhaMessage
{
    [Field(0)] public long MemberId { get; set; }
    [Field(1, IsVarInt = true)] public int NewRole { get; set; }

    public override int GetMessageType()
    {
        return 14306;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}