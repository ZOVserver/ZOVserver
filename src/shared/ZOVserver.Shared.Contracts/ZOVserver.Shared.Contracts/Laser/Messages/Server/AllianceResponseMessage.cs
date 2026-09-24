using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class AllianceResponseMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int Response { get; set; }
    [Field(1, IsVarInt = true)] public int UnkVInt { get; set; }

    public override int GetMessageType()
    {
        return 24333;
    }

    public override int GetServiceNodeType()
    {
        return 11;
    }
}