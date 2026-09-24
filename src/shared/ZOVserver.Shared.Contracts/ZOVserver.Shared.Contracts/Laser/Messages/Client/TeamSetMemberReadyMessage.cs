using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamSetMemberReadyMessage : PiranhaMessage
{
    [Field(0)] public bool IsReady { get; set; }
    [Field(1, IsVarInt = true)] public int UnkVInt { get; set; }

    public override int GetMessageType()
    {
        return 14355;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}