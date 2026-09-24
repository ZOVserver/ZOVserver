using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamMemberStatusMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int Status { get; set; }

    public override int GetMessageType()
    {
        return 14361;
    }

    public override int GetServiceNodeType()
    {
        return 23;
    }
}