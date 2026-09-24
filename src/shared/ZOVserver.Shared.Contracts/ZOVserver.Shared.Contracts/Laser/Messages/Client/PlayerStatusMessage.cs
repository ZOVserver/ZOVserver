using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class PlayerStatusMessage : PiranhaMessage
{
    [Field(0, IsVarInt = true)] public int Status { get; set; }

    public override int GetMessageType()
    {
        return 14366;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}