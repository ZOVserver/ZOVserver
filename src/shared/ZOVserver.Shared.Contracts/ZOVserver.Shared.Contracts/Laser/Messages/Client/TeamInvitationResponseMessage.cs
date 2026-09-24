using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamInvitationResponseMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int Response { get; set; }
    [Field(1)] public long TeamId { get; set; }
    [Field(2)] public bool MutePlayer { get; set; }

    public override int GetMessageType()
    {
        return 14479;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}