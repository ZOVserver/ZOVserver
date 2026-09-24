using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class TeamErrorMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int Unk1 { get; set; }
    [Field(1, IsVarInt = true)] public int ErrorCode { get; set; }

    public override int GetMessageType()
    {
        return 24129;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}