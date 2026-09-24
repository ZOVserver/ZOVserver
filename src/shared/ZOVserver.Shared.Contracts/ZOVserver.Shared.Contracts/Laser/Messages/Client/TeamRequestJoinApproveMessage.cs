using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamRequestJoinApproveMessage : PiranhaMessage
{
    [Field(0)] public long JoinerId { get; set; }
    [Field(1)] public bool State { get; set; }
    [Field(2)] public bool MutePlayer { get; set; }

    public override int GetMessageType()
    {
        return 14882;
    }

    public override int GetServiceNodeType()
    {
        return 23;
    }
}