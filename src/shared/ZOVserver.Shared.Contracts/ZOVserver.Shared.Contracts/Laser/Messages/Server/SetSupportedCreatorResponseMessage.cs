using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class SetSupportedCreatorResponseMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int ErrorCode { get; set; }

    [Field(1)] public string Code { get; set; } = string.Empty;

    public override int GetMessageType()
    {
        return 28686;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}