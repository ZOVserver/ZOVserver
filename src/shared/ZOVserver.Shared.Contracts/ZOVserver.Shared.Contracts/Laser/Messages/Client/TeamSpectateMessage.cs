using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class TeamSpectateMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public long TeamId { get; set; }
    [Field(1, IsVarInt = true)] public int UnkVInt { get; set; }

    public override int GetMessageType()
    {
        return 14358;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}